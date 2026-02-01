using Aizen.Modules.Identity.Domain.Entities;

namespace Aizen.Modules.Identity.Domain.Interface
{
    public interface IUserLoginTokenRepository
    {
        /// <summary>
        /// Belirli bir kullanıcı için tüm tokenları getirir.
        /// </summary>
        Task<List<UserLoginTokenEntity>> GetAllTokensByUserIdAsync(long userId);

        /// <summary>
        /// Kullanıcının son geçerli token'ını getirir (revoke edilmemiş ve süresi dolmamış).
        /// </summary>
        Task<UserLoginTokenEntity?> GetLatestValidTokenAsync(long userId);

        /// <summary>
        /// AccessToken değeriyle token kaydını getirir.
        /// </summary>
        Task<UserLoginTokenEntity?> GetByAccessTokenAsync(string accessToken);

        /// <summary>
        /// RefreshToken değeriyle token kaydını getirir.
        /// </summary>
        Task<UserLoginTokenEntity?> GetByRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Yeni token kaydı ekler.
        /// </summary>
        Task AddAsync(UserLoginTokenEntity token);

        /// <summary>
        /// Var olan token kaydını günceller (refresh, revoke vb).
        /// </summary>
        void Update(UserLoginTokenEntity token);

        /// <summary>
        /// Aynı kullanıcıya ait en fazla 2 token saklanabilir. Geri kalanlarını revoke eder ve temizler.
        /// </summary>
        Task RevokeOldTokensAsync(long userId, int maxTokenCount = 2);

        /// <summary>
        /// Kullanıcının tüm tokenlarını revoke eder (logout all).
        /// </summary>
        Task RevokeAllTokensAsync(long userId);

        /// <summary>
        /// Belirli bir cihaz ID ile ilişkili token var mı kontrol eder.
        /// </summary>
        Task<bool> ExistsByDeviceAsync(long userId, string deviceId);
    }
}
