using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Aizen.Core.InfoAccessor.Abstraction;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json.Linq;
using Aizen.Core.Infrastructure.Exception;
using System.Security.Claims;

namespace Aizen.Core.InfoAccessor.Middlewares
{
    internal class AizenUserInfoMiddleware
    {
        private readonly RequestDelegate _next;

        public AizenUserInfoMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var infoAccessor = (IAizenInfoAccessor)httpContext.RequestServices.GetRequiredService(typeof(IAizenInfoAccessor));

            if (infoAccessor.UserInfoAccessor.UserInfo != null)
            {
                await _next(httpContext);
                return;
            }

            string token = null;

            if (httpContext?.Request?.Path.Value?.Contains("auth/refresh") == true)
            {
                httpContext.Request.EnableBuffering();
                using (var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true))
                {
                    var body = await reader.ReadToEndAsync();
                    var jObject = JObject.Parse(body);
                    token = jObject["accessToken"]?.ToString();
                    httpContext.Request.Body.Position = 0;
                }
            }
            else if (httpContext?.Request?.Headers != null && httpContext.Request.Headers.ContainsKey("Authorization"))
            {
                token = httpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            }

            var container = (IAizenInfoContainer)httpContext.RequestServices.GetRequiredService(typeof(IAizenInfoContainer));

            if (string.IsNullOrEmpty(token))
            {
                container.Set(new AizenUserInfo());
                await _next(httpContext);
                return;
            }

            if (IsKeycloakClientToken(token, out var keycloakTokenInfo))
            {
                // Keycloak API/service-account  do not parse as application user.token 
                container.Set(keycloakTokenInfo);
                container.Set(new AizenUserInfo());
            }
            else
            {
                var isTokenValid = TryGetUserInfoFromToken(token, out AizenUserInfo userInfo, out string errorMessage);
                if (!isTokenValid)
                {
                    throw new AizenBusinessException(errorMessage);
                }

                container.Set(userInfo);
            }

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
