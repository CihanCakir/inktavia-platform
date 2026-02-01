using System.Collections.Generic;
using System.Threading.Tasks;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;

namespace Aizen.Modules.Identity.Domain.Interface
{
    public interface IUserDeviceRepository
    {
        /// <summary>
        /// Kullanıcının cihaz bilgisini ekler veya günceller.
        /// </summary>
        Task AddOrUpdateDeviceAsync(
            long userId,
            string deviceId,
            string? notificationToken,
            ConsumerDeviceType deviceType,
            WorkshopRoleContext roleContext,
            long? activeProfileId
        );

        /// <summary>
        /// Kullanıcının belirli bir cihazı sistemde kayıtlı mı kontrol eder.
        /// </summary>
        Task<bool> IsDeviceExistsAsync(long userId, string deviceId);

        /// <summary>
        /// Kullanıcının tüm cihazlarını pasif duruma getirir.
        /// </summary>
        Task RevokeAllDevicesAsync(long userId);

        /// <summary>
        /// Kullanıcının aktif cihazlarını getirir.
        /// </summary>
        Task<List<UserDeviceEntity>> GetActiveDevicesByUserIdAsync(long userId);


        /// <summary>
        /// Bu cihaz belirtilen günde zaten engellenmiş mi?
        /// </summary>
        Task<bool> IsDeviceBlockedTodayAsync(string deviceId, DateTime date);

        /// <summary>
        /// Belirtilen cihazdan bugün giriş yapan kullanıcı sayısını döner
        /// </summary>
        Task<int> GetTodayLoginUserCountByDeviceIdAsync(string deviceId, DateTime date);

        /// <summary>
        /// Yeni blok kaydı oluşturur
        /// </summary>
        Task AddDeviceBlockAsync(UserDeviceBlockEntity entity);
    }
}
