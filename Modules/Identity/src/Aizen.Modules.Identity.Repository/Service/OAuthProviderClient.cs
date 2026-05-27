using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Aizen.Modules.Identity.Repository.Identity.Service
{
    public sealed class OAuthProviderClient : IOAuthProviderClient
    {
        private readonly OAuthOptions _opt;
        private readonly HttpClient _http;
        private readonly IAizenDistributedCache _cache;
        private readonly ILogger<OAuthProviderClient> _logger;

        public OAuthProviderClient(IOptions<OAuthOptions> opt, IHttpClientFactory factory, IAizenDistributedCache cache, ILogger<OAuthProviderClient> logger)
        {
            _opt = opt.Value;
            _http = factory.CreateClient(nameof(OAuthProviderClient));
            _cache = cache;
            _logger = logger;
        }

        // === START: authorize URL ===
        public string GetAuthorizeUrl(string provider, string state, string nonce, string codeChallenge, string? uiLocale, string? redirectAfterLogin)
        {
            if (IsGoogle(provider))
            {
                var g = _opt.Google;
                var qp = new QueryString()
                    .Add("client_id", g.ClientId)
                    .Add("redirect_uri", _opt.PublicBaseUrl + g.CallbackPath)
                    .Add("response_type", "code")
                    .Add("scope", string.Join(' ', g.Scopes))
                    .Add("state", state)
                    .Add("nonce", nonce)
                    .Add("code_challenge", codeChallenge)
                    .Add("code_challenge_method", "S256")
                    .Add("access_type", "online")
                    .Add("include_granted_scopes", "true");
                if (!string.IsNullOrEmpty(uiLocale)) qp = qp.Add("hl", uiLocale);
                if (!string.IsNullOrEmpty(redirectAfterLogin)) qp = qp.Add("prompt", "select_account"); // örnek
                return _opt.Google.AuthorizeEndpoint + qp;
            }
            else // Apple
            {
                var a = _opt.Apple;
                var qp = new QueryString()
                    .Add("response_type", "code")
                    .Add("response_mode", "form_post")
                    .Add("client_id", a.ClientId)
                    .Add("redirect_uri", _opt.PublicBaseUrl + a.CallbackPath)
                    .Add("scope", string.Join(' ', a.Scopes))
                    .Add("state", state)
                    .Add("nonce", nonce)
                    .Add("code_challenge", codeChallenge)
                    .Add("code_challenge_method", "S256");
                if (!string.IsNullOrEmpty(uiLocale)) qp = qp.Add("ui_locales", uiLocale);
                return _opt.Apple.AuthorizeEndpoint + qp;
            }
        }

        // === CALLBACK: code exchange ===
        public async Task<OAuthTokenExchangeResponse> ExchangeAsync(string provider, string code, string? codeVerifier, CancellationToken ct)
        {
            if (IsGoogle(provider))
            {
                var g = _opt.Google;
                var dict = new Dictionary<string, string?>
                {
                    ["client_id"] = g.ClientId,
                    ["client_secret"] = g.ClientSecret,
                    ["code"] = code,
                    ["grant_type"] = "authorization_code",
                    ["redirect_uri"] = _opt.PublicBaseUrl + g.CallbackPath,
                    ["code_verifier"] = codeVerifier
                };
                using var resp = await _http.PostAsync(g.TokenEndpoint, new FormUrlEncodedContent(dict!), ct);
                var json = await resp.Content.ReadAsStringAsync(ct);
                if (!resp.IsSuccessStatusCode) throw new SecurityException($"Google token exchange failed: {json}");
                var doc = JsonDocument.Parse(json);
                var idToken = doc.RootElement.GetProperty("id_token").GetString()!;
                var scope = doc.RootElement.TryGetProperty("scope", out var s) ? s.GetString() : null;
                return new OAuthTokenExchangeResponse(idToken, scope);
            }
            else
            {
                var a = _opt.Apple;
                var clientSecret = CreateAppleClientSecret(); // kısa ömürlü
                var dict = new Dictionary<string, string?>
                {
                    ["client_id"] = a.ClientId,
                    ["client_secret"] = clientSecret,
                    ["code"] = code,
                    ["grant_type"] = "authorization_code",
                    ["redirect_uri"] = _opt.PublicBaseUrl + a.CallbackPath,
                    ["code_verifier"] = codeVerifier
                };
                using var resp = await _http.PostAsync(a.TokenEndpoint, new FormUrlEncodedContent(dict!), ct);
                var json = await resp.Content.ReadAsStringAsync(ct);
                if (!resp.IsSuccessStatusCode) throw new SecurityException($"Apple token exchange failed: {json}");
                var doc = JsonDocument.Parse(json);
                var idToken = doc.RootElement.GetProperty("id_token").GetString()!;
                // Apple scope dönmez; null bırakıyoruz
                return new OAuthTokenExchangeResponse(idToken, null);
            }
        }

        // === CALLBACK: id_token doğrulama (JWKS + nonce/aud/iss) ===
        public async Task<ValidatedIdTokenDto> ValidateIdTokenAsync(string provider, string idToken, string expectedNonce, CancellationToken ct)
        {
            var (issuer, audience, jwksUrl) = IsGoogle(provider)
                ? (_opt.Google.Issuer, _opt.Google.ClientId, "https://www.googleapis.com/oauth2/v3/certs")
                : (_opt.Apple.Issuer, _opt.Apple.ClientId, "https://appleid.apple.com/auth/keys");

            var cacheKey = "jwks:" + jwksUrl;

            // 1) JWKS: cache → http fetch (+ cache set)
            JsonWebKeySet keys;
            var (hit, cached) = await _cache.TryGetAsync<JsonWebKeySet>(cacheKey, ct);
            if (hit && cached is not null)
            {
                keys = cached;
            }
            else
            {
                var txt = await _http.GetStringAsync(jwksUrl, ct);
                keys = new JsonWebKeySet(txt);

                await _cache.SetAsync(
                    cacheItem: keys,
                    key: cacheKey,
                    cacheOptions: new AizenCacheOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) },
                    token: ct
                );
            }

            // 2) id_token doğrulama
            var handler = new JsonWebTokenHandler();
            var tvp = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeyResolver = (_, securityToken, kid, _) =>
                    keys.GetSigningKeys().Where(k => (k is JsonWebKey jwk) ? jwk.Kid == kid : true),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var result = handler.ValidateToken(idToken, tvp);
            if (!result.IsValid)
                throw new SecurityException($"id_token invalid: {result.Exception?.Message}");

            var jwt = (JsonWebToken)result.SecurityToken!;
            var nonce = jwt.TryGetPayloadValue("nonce", out string? n) ? n : null;
            if (!string.Equals(nonce, expectedNonce, StringComparison.Ordinal))
                throw new SecurityException("nonce mismatch");

            var sub = jwt.Subject!;
            jwt.TryGetPayloadValue("email", out string? email);

            bool emailVerified = false;
            if (jwt.TryGetPayloadValue("email_verified", out object? ev))
                emailVerified = ev switch { bool b => b, string s => s is "true" or "1", _ => false };

            string? name = null;
            jwt.TryGetPayloadValue("name", out name);

            return new ValidatedIdTokenDto(
                Sub: sub,
                Email: email,
                EmailVerified: emailVerified,
                Name: name,
                Issuer: jwt.Issuer,
                Nonce: nonce!
            );
        }

        // === Apple client_secret (ES256) ===
        private string CreateAppleClientSecret()
        {
            var a = _opt.Apple;
            var now = DateTimeOffset.UtcNow;

            // p8 → ECDsa
            using var ecdsa = LoadECDsaFromAppleP8(a.P8PrivateKeyPem);

            var creds = new SigningCredentials(new ECDsaSecurityKey(ecdsa) { KeyId = a.KeyId }, SecurityAlgorithms.EcdsaSha256);
            var desc = new SecurityTokenDescriptor
            {
                Issuer = a.TeamId,                           // iss
                Audience = "https://appleid.apple.com",      // aud sabit
                Subject = new ClaimsIdentity(new[] { new Claim("sub", a.ClientId) }), // sub = client_id
                IssuedAt = now.UtcDateTime,
                Expires = now.AddMinutes(10).UtcDateTime,
                SigningCredentials = creds
            };
            return new JsonWebTokenHandler().CreateToken(desc);
        }

        private static ECDsa LoadECDsaFromAppleP8(string pem)
        {
            // -----BEGIN PRIVATE KEY----- PKCS8
            pem = pem.Replace("\r", "").Trim();
            var base64 = pem.Replace("-----BEGIN PRIVATE KEY-----", "")
                            .Replace("-----END PRIVATE KEY-----", "")
                            .Replace("\n", "").Trim();
            var pkcs8 = Convert.FromBase64String(base64);
            var ecdsa = ECDsa.Create();
            ecdsa.ImportPkcs8PrivateKey(pkcs8, out _);
            return ecdsa;
        }

        private static bool IsGoogle(string provider) => provider.Equals("google", StringComparison.OrdinalIgnoreCase);


        private async Task<JsonWebKeySet> GetOrFetchJwksAsync(string jwksUrl, CancellationToken ct)
        {
            var cacheKey = "jwks:" + jwksUrl;
            var (hit, cached) = await _cache.TryGetAsync<JsonWebKeySet>(cacheKey, ct);
            if (hit && cached is not null) return cached;

            var txt = await _http.GetStringAsync(jwksUrl, ct);
            var keys = new JsonWebKeySet(txt);
            await _cache.SetAsync(keys, cacheKey, new AizenCacheOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) }, ct);
            return keys;
        }

        Task<OAuthTokenExchangeResponse> IOAuthProviderClient.ExchangeAsync(string provider, string code, string? codeVerifier, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        Task<ValidatedIdTokenDto> IOAuthProviderClient.ValidateIdTokenAsync(string provider, string idToken, string expectedNonce, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }

}