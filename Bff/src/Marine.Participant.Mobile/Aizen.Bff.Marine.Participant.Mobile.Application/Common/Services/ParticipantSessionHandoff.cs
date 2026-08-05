using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Options;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;

/// <summary>
/// Native ticket→session handoff. The mobile app is not a browser, so the BFF runs the OIDC
/// authorization-code + PKCE exchange server-side against the PUBLIC <c>inktavia-mobile</c> client
/// (the client the M2b SPI flow is bound to). Given a single-use Identity <c>login_ticket</c> it:
///   1) GETs the Keycloak authorize endpoint with <c>login_ticket</c> (SPI authenticates the user and
///      302s to the registered redirect_uri carrying <c>code</c>);
///   2) POSTs the token endpoint (grant_type=authorization_code + <c>code_verifier</c>) for real tokens.
/// No confidential secret is used for this USER token exchange (public client + PKCE).
/// </summary>
public interface IParticipantSessionHandoff
{
    Task<HandoffTokens> ExchangeAsync(string loginTicket, CancellationToken ct = default);
}

public sealed class HandoffTokens
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

internal sealed class ParticipantSessionHandoff : IParticipantSessionHandoff
{
    /// <summary>Named HttpClient (kept for DI compatibility); the handoff constructs its own client so that
    /// auto-redirect is guaranteed OFF regardless of any HttpClientFactory default handler.</summary>
    public const string HttpClientName = "MarineMobileKeycloakOidc";

    private readonly MarineMobileKeycloakOptions _options;
    private readonly ILogger<ParticipantSessionHandoff> _logger;

    public ParticipantSessionHandoff(
        IOptions<MarineMobileKeycloakOptions> options,
        ILogger<ParticipantSessionHandoff> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HandoffTokens> ExchangeAsync(string loginTicket, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(loginTicket))
            throw new AizenBusinessException("Sign-in could not be completed. Please request a new code.");

        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = ComputeS256Challenge(codeVerifier);
        var state = GenerateCodeVerifier(); // reuse the random generator; state only needs to be opaque
        var redirectUri = _options.BffRedirectUri;

        // Own handler with auto-redirect OFF + isolated cookies — the BFF must READ the authorize 302
        // Location itself (auto-follow would chase the unreachable redirect_uri host and fail).
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = true,
            CookieContainer = new System.Net.CookieContainer(),
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };

        var code = await FollowAuthorizeToCodeAsync(client, codeChallenge, state, redirectUri, loginTicket, ct);
        if (string.IsNullOrEmpty(code))
        {
            _logger.LogWarning("OTP handoff: authorize did not yield an authorization code (login_ticket rejected or user unresolved).");
            throw new AizenBusinessException("Sign-in could not be completed. Please request a new code.");
        }

        return await ExchangeCodeForTokensAsync(client, code, codeVerifier, redirectUri, ct);
    }

    // Drive the authorize endpoint manually (no auto-redirect), following any intra-Keycloak hops while
    // carrying cookies, until we hit the external redirect_uri carrying ?code= (success) or ?error= (fail).
    private async Task<string?> FollowAuthorizeToCodeAsync(
        HttpClient client, string codeChallenge, string state, string redirectUri, string loginTicket, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _options.ClientId,
            ["response_type"] = "code",
            ["scope"] = "openid",
            ["redirect_uri"] = redirectUri,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
            ["login_ticket"] = loginTicket,
        };
        var url = QueryHelpersAddQueryString(_options.AuthorizeEndpoint, query);

        for (var hop = 0; hop < 6; hop++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (resp.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found
                or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect)
            {
                var location = resp.Headers.Location?.ToString();
                if (string.IsNullOrEmpty(location)) return null;

                if (location.StartsWith(redirectUri, StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri(location);
                    var parsed = ParseQuery(uri.Query);
                    if (parsed.TryGetValue("error", out var err))
                    {
                        _logger.LogWarning("OTP handoff: authorize returned error '{Error}'.", err);
                        return null;
                    }
                    return parsed.TryGetValue("code", out var code) ? code : null;
                }

                // Intra-Keycloak hop (e.g. login-actions) — resolve relative + continue with cookies.
                url = new Uri(new Uri(_options.AuthorizeEndpoint), location).ToString();
                continue;
            }

            // 200 = a login page was rendered (SPI did not authenticate) or an error page — not a success.
            _logger.LogWarning("OTP handoff: authorize returned {Status} at hop {Hop} (no redirect to code).", (int)resp.StatusCode, hop);
            return null;
        }

        _logger.LogWarning("OTP handoff: too many redirects while resolving the authorization code.");
        return null;
    }

    private async Task<HandoffTokens> ExchangeCodeForTokensAsync(
        HttpClient client, string code, string codeVerifier, string redirectUri, CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _options.ClientId,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier,
        });

        using var resp = await client.PostAsync(_options.TokenEndpoint, content, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("OTP handoff: token exchange failed with {Status}.", (int)resp.StatusCode);
            throw new AizenBusinessException("Sign-in could not be completed. Please request a new code.");
        }

        var token = await resp.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: ct);
        if (token is null || string.IsNullOrEmpty(token.AccessToken))
            throw new AizenBusinessException("Sign-in could not be completed. Please request a new code.");

        return new HandoffTokens
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken ?? string.Empty,
            ExpiresIn = token.ExpiresIn,
            TokenType = string.IsNullOrEmpty(token.TokenType) ? "Bearer" : token.TokenType,
        };
    }

    // ── PKCE + small URL helpers (no external deps) ──────────────────────────────

    private static string GenerateCodeVerifier()
        => Base64Url(RandomNumberGenerator.GetBytes(40));

    private static string ComputeS256Challenge(string verifier)
        => Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string QueryHelpersAddQueryString(string uri, Dictionary<string, string?> query)
    {
        var sb = new StringBuilder(uri);
        var first = !uri.Contains('?');
        foreach (var kv in query)
        {
            if (kv.Value is null) continue;
            sb.Append(first ? '?' : '&');
            first = false;
            sb.Append(Uri.EscapeDataString(kv.Key)).Append('=').Append(Uri.EscapeDataString(kv.Value));
        }
        return sb.ToString();
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq < 0) { result[Uri.UnescapeDataString(part)] = string.Empty; continue; }
            result[Uri.UnescapeDataString(part[..eq])] = Uri.UnescapeDataString(part[(eq + 1)..]);
        }
        return result;
    }

    private sealed class KeycloakTokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = default!;
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("token_type")] public string? TokenType { get; set; }
    }
}
