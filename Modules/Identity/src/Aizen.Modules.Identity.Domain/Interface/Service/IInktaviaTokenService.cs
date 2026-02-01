using System.Security.Claims;
using Aizen.Modules.Identity.Abstraction.Dto;

namespace Aizen.Modules.Identity.Domain.Interface
{
    public interface IInktaviaTokenService
    {
        /// <summary>
        /// Kullanıcı bilgilerine göre Access & Refresh token üretir
        /// </summary>
        AccessTokenDto GenerateToken(long userId, string username, List<string> roles);

        /// <summary>
        /// AccessToken süresi dolmadan önce yenileme amaçlı RefreshToken kontrolü ve yeniden üretim
        /// </summary>
        Task<AccessTokenDto> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Token içerisindeki Claims verilerini çözer
        /// </summary>
        ClaimsPrincipal? GetPrincipalFromAccessToken(string accessToken);

        /// <summary>
        /// Rastgele yeni bir Refresh Token üretir
        /// </summary>
        string GenerateRefreshToken();

        /// <summary>
        /// Access token'ın süresini verir
        /// </summary>
        DateTime GetAccessTokenExpiration();

        /// <summary>
        /// Refresh token'ın süresini verir
        /// </summary>
        DateTime GetRefreshTokenExpiration();
    }

}