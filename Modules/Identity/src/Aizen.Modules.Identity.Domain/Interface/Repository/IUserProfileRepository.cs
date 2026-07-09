using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;

namespace Aizen.Modules.Identity.Domain.Interface
{
    public interface IUserProfileRepository
    {
        /// <summary>
        /// Belirtilen kullanıcı ve role context için aktif profilin ID'sini döner.
        /// </summary>
        Task<UserProfileEntity?> GetActiveProfileIdAsync(long userId, WorkshopRoleContext roleContext);
        

        /// <summary>
        /// Belirtilen kullanıcı için belirtilen role context'e sahip profil var mı kontrol eder.
        /// </summary>
        Task<bool> HasProfileForContextAsync(long userId, WorkshopRoleContext roleContext);

        /// <summary>
        /// Belirli bir kullanıcı için tüm profilleri döner.
        /// </summary>
        Task<List<UserProfileEntity>> GetAllProfilesAsync(long userId);

        /// <summary>
        /// Belirli bir kullanıcı için spesifik bir profilin detaylarını döner.
        /// </summary>
        Task<UserProfileEntity?> GetProfileByIdAsync(long profileId);

        /// <summary>
        /// Verilen Keycloak subject'ine bağlı Organizer profilini döner; bağlı değilse null.
        /// </summary>
        Task<UserProfileEntity?> GetOrganizerProfileByKeycloakSubjectAsync(
            string keycloakSubjectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Yeni bir kullanıcı profili oluşturur.
        /// </summary>
        Task AddProfileAsync(UserProfileEntity profile);

        /// <summary>
        /// Bir kullanıcı profilini günceller.
        /// </summary>
        void UpdateProfileAsync(UserProfileEntity profile);
    }
}