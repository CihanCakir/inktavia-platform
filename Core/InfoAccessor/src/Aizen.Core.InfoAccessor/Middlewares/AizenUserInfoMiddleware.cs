using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Aizen.Core.InfoAccessor.Abstraction;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Aizen.Core.InfoAccessor.Middlewares
{
    internal class AizenUserInfoMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AizenUserInfoMiddleware> _logger;

        public AizenUserInfoMiddleware(RequestDelegate next, ILogger<AizenUserInfoMiddleware> logger)
        {
            this._next = next;
            this._logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var infoAccessor = (IAizenInfoAccessor)httpContext.RequestServices.GetRequiredService(typeof(IAizenInfoAccessor));

            if (infoAccessor.UserInfoAccessor.UserInfo != null)
            {
                await _next(httpContext);
                return;
            }

            var container = (IAizenInfoContainer)httpContext.RequestServices.GetRequiredService(typeof(IAizenInfoContainer));

            // Detect and register Keycloak/service-account token from Authorization header.
            AizenKeycloakTokenInfo keycloakTokenInfo = null;
            if (httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authToken = authHeader.ToString().Replace("Bearer ", "");
                if (IsKeycloakClientToken(authToken, out keycloakTokenInfo))
                {
                    container.Set(keycloakTokenInfo);
                }
            }

            // Read the application/identity user token exclusively from X-Aizen-User-Token.
            if (!httpContext.Request.Headers.TryGetValue(AizenAuthHeaders.UserToken, out var userTokenHeader)
                || string.IsNullOrWhiteSpace(userTokenHeader))
            {
                // Provider path: accept a trusted-BFF identity assertion (shared-secret gated) when present.
                // Disabled by default (empty BffAssertion:SharedSecret) → behaves exactly as before.
                if (TryAcceptBffAssertion(httpContext, keycloakTokenInfo, out var assertedInfo, out var assertedProfileId))
                {
                    container.Set(assertedInfo);
                    if (keycloakTokenInfo != null && assertedProfileId.HasValue)
                    {
                        keycloakTokenInfo.ProviderProfileId = assertedProfileId;
                        container.Set(keycloakTokenInfo);
                    }

                    await _next(httpContext);
                    return;
                }

                container.Set(new AizenUserInfo());
                await _next(httpContext);
                return;
            }
            if (userTokenHeader.ToString().Contains("Bearer "))
            {
                userTokenHeader = userTokenHeader.ToString().Replace("Bearer ", "");
            }

            var isTokenValid = TryGetUserInfoFromToken(userTokenHeader.ToString(), out AizenUserInfo userInfo, out string errorMessage);
            if (!isTokenValid)
            {
                // Graceful degradation: log a warning instead of throwing.
                // The Keycloak service token already validates caller authorization.
                // Domain-level checks that require a valid UserId will still reject unauthorized access.
                _logger.LogWarning(
                    "[AizenUserInfoMiddleware] X-Aizen-User-Token could not be validated: {ErrorMessage}. " +
                    "Continuing with empty UserInfo. Path={Path}",
                    errorMessage,
                    httpContext.Request.Path);
                container.Set(new AizenUserInfo());
                await _next(httpContext);
                return;
            }

            container.Set(userInfo);
            await _next(httpContext);
        }

        /// <summary>
        /// Returns true when <paramref name="token"/> is a Keycloak API/client credential token.
        /// Detection: valid JWT that does NOT carry the application-specific <c>UserId</c> claim.
        /// </summary>
        private static bool IsKeycloakClientToken(string token, out AizenKeycloakTokenInfo keycloakTokenInfo)
        {
            keycloakTokenInfo = null;

            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token))
                return false;

            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
            if (jwtToken == null)
                return false;

            var hasUserId = jwtToken.Claims.Any(c => c.Type == "UserId");
            if (hasUserId)
                return false;

            keycloakTokenInfo = new AizenKeycloakTokenInfo
            {
                IsPresent = true,
                ClientId = ClaimVal(jwtToken, "azp"),
                Subject = ClaimVal(jwtToken, JwtRegisteredClaimNames.Sub),
                RawToken = token,
            };

            return true;
        }

        /// <summary>
        /// Accepts a trusted-BFF identity assertion: requires a Keycloak service-token caller AND a valid
        /// shared secret (X-Aizen-Bff-Assertion == BffAssertion:SharedSecret). Then populates UserInfo from
        /// X-Aizen-User-Id (and the optional provider profile id). Disabled when the secret is unset.
        /// </summary>
        private static bool TryAcceptBffAssertion(
            HttpContext httpContext,
            AizenKeycloakTokenInfo keycloakTokenInfo,
            out AizenUserInfo userInfo,
            out long? providerProfileId)
        {
            userInfo = null;
            providerProfileId = null;

            // Must be a machine-to-machine Keycloak service-token caller.
            if (keycloakTokenInfo is null || !keycloakTokenInfo.IsPresent)
                return false;

            var cfg = (httpContext.RequestServices.GetService(typeof(IOptions<AizenBffAssertionOptions>))
                as IOptions<AizenBffAssertionOptions>)?.Value;
            if (cfg is null || string.IsNullOrWhiteSpace(cfg.SharedSecret))
                return false; // feature disabled by default

            var provided = httpContext.Request.Headers[AizenAuthHeaders.BffAssertion].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(provided) || !FixedTimeEquals(provided, cfg.SharedSecret))
                return false;

            if (cfg.AllowedClientIds is { Length: > 0 } allow
                && !allow.Contains(keycloakTokenInfo.ClientId, StringComparer.OrdinalIgnoreCase))
                return false;

            var uidRaw = httpContext.Request.Headers[AizenAuthHeaders.AssertedUserId].FirstOrDefault();
            if (!long.TryParse(uidRaw, out var uid) || uid <= 0)
                return false;

            if (long.TryParse(httpContext.Request.Headers[AizenAuthHeaders.AssertedProfileId].FirstOrDefault(), out var pid)
                && pid > 0)
                providerProfileId = pid;

            userInfo = new AizenUserInfo
            {
                UserId = uid,
                AccessToken = keycloakTokenInfo.RawToken ?? string.Empty,
                PhoneNumber = string.Empty,
                Roles = System.Array.Empty<string>()
            };
            return true;
        }

        private static bool FixedTimeEquals(string a, string b)
            => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

        private bool TryGetUserInfoFromToken(string token, out AizenUserInfo userInfo, out string errorMessage)
        {
            userInfo = null;
            errorMessage = string.Empty;

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            if (jwtToken == null)
            {
                errorMessage = "Invalid token.";
                return false;
            }

            var refreshTokenExpireClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "RefreshTokenExpire")?.Value;
            if (refreshTokenExpireClaim == null)
            {
                errorMessage = "RefreshTokenExpire claim is missing.";
                return false;
            }

            if (!DateTime.TryParse(refreshTokenExpireClaim, out DateTime refreshTokenExpireDateTime))
            {
                errorMessage = "Invalid RefreshTokenExpire date format.";
                return false;
            }

            if (refreshTokenExpireDateTime <= DateTime.UtcNow)
            {
                errorMessage = "Refresh token has expired.";
                return false;
            }

            var userIdStr = ClaimVal(jwtToken, "UserId");
            var subjectStr = ClaimVal(jwtToken, JwtRegisteredClaimNames.Sub, ClaimTypes.Name, ClaimTypes.Email);

            var roles = jwtToken.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .Distinct()
                    .ToArray();

            userInfo = new AizenUserInfo
            {
                UserId = ParseLong(userIdStr),
                PhoneNumber = subjectStr ?? string.Empty,
                AccessToken = token,
                Roles = roles
            };

            return true;
        }

        static string ClaimVal(JwtSecurityToken t, params string[] types)
            => types.Select(tt => t.Claims.FirstOrDefault(c => c.Type == tt)?.Value)
                    .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        static long ParseLong(string s, long def = 0) => long.TryParse(s, out var v) ? v : def;
    }
}
