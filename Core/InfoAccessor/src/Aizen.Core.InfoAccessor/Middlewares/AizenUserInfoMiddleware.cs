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
            var seyirInfoAccessor = (IAizenInfoAccessor)httpContext.RequestServices.GetRequiredService(typeof(IAizenInfoAccessor));

            if (seyirInfoAccessor.UserInfoAccessor.UserInfo != null)
            {
                await _next(httpContext);
                return;
            }

            AizenUserInfo userInfo = null;
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

            if (!string.IsNullOrEmpty(token))
            {
                var isTokenValid = TryGetUserInfoFromToken(token, out userInfo, out string errorMessage);
                if (!isTokenValid)
                {
                    throw new AizenBusinessException(errorMessage);
                }
            }
            else
            {
                userInfo = new AizenUserInfo();
            }

            var container = (IAizenInfoContainer)httpContext.RequestServices.GetRequiredService(typeof(IAizenInfoContainer));
            container.Set(userInfo);

            await _next(httpContext);
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

            // Extract the RefreshTokenExpire claim and check expiration
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

            // --- claim’leri çek
            var userIdStr = ClaimVal(jwtToken, "UserId");
            var subjectStr = ClaimVal(jwtToken, JwtRegisteredClaimNames.Sub, ClaimTypes.Name, ClaimTypes.Email); // bizde sub=username/e-posta
            var jtiStr = ClaimVal(jwtToken, JwtRegisteredClaimNames.Jti, "jti");
            var versionStr = ClaimVal(jwtToken, "Version");
            var rtExpStr = ClaimVal(jwtToken, "RefreshTokenExpire");

            var roles = jwtToken.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .Distinct()
                    .ToArray();

            var refreshExp = ParseRefreshExp(rtExpStr);

            // Extract other user info from token
            userInfo = new AizenUserInfo
            {
                UserId = ParseLong(userIdStr),
                PhoneNumber = subjectStr ?? string.Empty,
                AccessToken = token,
                // Aşağıdakiler opsiyonel alanlar ise (sende varsa doldururuz):
                Roles = roles                         // class’ta IEnumerable<string> Roles varsa
                                                      // RoleId yok — roller isim olarak geliyor. Gerekirse ilk rolü map’leyip RoleId türetebilirsin.
            };

            return true;
        }



        static string? ClaimVal(JwtSecurityToken t, params string[] types)
            => types.Select(tt => t.Claims.FirstOrDefault(c => c.Type == tt)?.Value)
                    .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        static long ParseLong(string? s, long def = 0) => long.TryParse(s, out var v) ? v : def;

        static DateTimeOffset? ParseRefreshExp(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return null;

            // Önce ISO/Tarih biçimleri
            if (DateTimeOffset.TryParse(v, out var dto)) return dto;

            // epoch seconds olarak geldiyse
            if (long.TryParse(v, out var sec) && sec > 1_000_000)
                return DateTimeOffset.FromUnixTimeSeconds(sec);

            // ticks olarak geldiyse
            if (long.TryParse(v, out var ticks) && ticks > 62_000_000_000_000) // rough guard
                return new DateTimeOffset(ticks, TimeSpan.Zero);

            return null;
        }
    }
}
