using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Aizen.Core.Auth.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Domain.Interface;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Aizen.Modules.Identity.Repository.Identity.Service
{

    public class InktaviaTokenService : IInktaviaTokenService
    {
        private readonly TokenSettings _tokenSettings;

        public InktaviaTokenService(IOptions<TokenSettings> tokenSettings)
        {
            _tokenSettings = tokenSettings.Value;
        }

        public AccessTokenDto GenerateToken(long userId, string username, List<string> roles)
        {
            var refreshTokenExpire = GetRefreshTokenExpiration();
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim("UserId", userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("Version", "V1"),
                new Claim("RefreshTokenExpire", refreshTokenExpire.ToString())
            };

            // Add user roles to claims
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenSettings.SecurityKey));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenExpiration = GetAccessTokenExpiration();
            var jwtToken = new JwtSecurityToken(
                issuer: _tokenSettings.Issuer,
                audience: _tokenSettings.Audience,
                claims: claims,
                expires: tokenExpiration,
                signingCredentials: signingCredentials);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            var refreshToken = GenerateRefreshToken();

            return new AccessTokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = tokenExpiration,
                RefreshTokenExpiresAt = refreshTokenExpire
            };
        }



        public Task<AccessTokenDto> RefreshTokenAsync(string refreshToken)
        {
            // NOT: Refresh token validation burada yapılmalı (veritabanı ya da cache ile)
            // Bu örnek doğrudan yeni token üretir

            // TODO: Refresh token doğrulama eklenmeli

            throw new NotImplementedException("RefreshToken doğrulama mekanizması henüz entegre edilmedi.");
        }

        public ClaimsPrincipal? GetPrincipalFromAccessToken(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _tokenSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _tokenSettings.Audience,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenSettings.SecurityKey)),
            };

            try
            {
                var principal = tokenHandler.ValidateToken(accessToken, validationParameters, out var validatedToken);
                if (validatedToken is JwtSecurityToken jwtToken &&
                    jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                {
                    return principal;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
        
        public DateTime GetAccessTokenExpiration()
        {
            return DateTime.UtcNow.AddMinutes(_tokenSettings.AccessTokenExpiration);
        }

        public DateTime GetRefreshTokenExpiration()
        {
            return DateTime.UtcNow.AddMinutes(_tokenSettings.RefreshTokenExpiration);
        }

    }
}