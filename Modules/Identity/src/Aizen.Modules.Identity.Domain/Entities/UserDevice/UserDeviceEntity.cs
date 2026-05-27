using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.Domain;
using Aizen.Modules.Identity.Abstraction;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserDeviceEntity : AizenEntityWithAudit
    {
        public long UserId { get; set; }
        public virtual UserEntity User { get; set; } = null!;

        public string DeviceId { get; set; } = null!; // Benzersiz cihaz tanımı (UUID, hash, vs.)

        public string? NotificationToken { get; set; } // FCM veya benzeri için push token

        public ConsumerDeviceType? ConsumerDeviceType { get; set; } // Android, iOS, Web, vs.

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginDate { get; set; }

        public WorkshopRoleContext RoleContext { get; set; } // Hangi rol bağlamında bu cihazda oturum açılmış?
        public long? ActiveProfileId { get; set; } // Bağlamda kullanılan profil ID


        /// <summary>
        /// Kullanıcıya ait yeni bir cihaz oluşturur.
        /// </summary>
        public static UserDeviceEntity Create(
            long userId,
            string deviceId,
            ConsumerDeviceType deviceType,
            string? notificationToken,
            WorkshopRoleContext roleContext,
            long? activeProfileId)
        {
            return new UserDeviceEntity
            {
                UserId = userId,
                DeviceId = deviceId,
                ConsumerDeviceType = deviceType,
                NotificationToken = notificationToken ?? "EmptyNotificationToken",
                RoleContext = roleContext,
                ActiveProfileId = activeProfileId,
                IsActive = true,
                LastLoginDate = DateTime.UtcNow
            };
        }


        /// <summary>
        /// Cihaz bilgilerini günceller (örn. push token değişti, login oldu).
        /// </summary>
        public void UpdateLoginInfo(string? newNotificationToken, ConsumerDeviceType newDeviceType, WorkshopRoleContext roleContext, long? activeProfileId)
        {
            NotificationToken = string.IsNullOrEmpty(newNotificationToken) ? "EmptyNotificationToken" : newNotificationToken;
            ConsumerDeviceType = newDeviceType;
            RoleContext = roleContext;
            ActiveProfileId = activeProfileId;
            LastLoginDate = DateTime.UtcNow;
            IsActive = true;
        }


        /// <summary>
        /// Cihaz oturumunu sonlandırır (cihaz devre dışı).
        /// </summary>
        public void Revoke()
        {
            IsActive = false;
        }


        /// <summary>
        /// Cihaz tekrar kullanılmaya başlandığında aktive edilir.
        /// </summary>
        public void Reactivate(string? newNotificationToken)
        {
            IsActive = true;
            NotificationToken = string.IsNullOrEmpty(newNotificationToken) ? "EmptyNotificationToken" : newNotificationToken;
            LastLoginDate = DateTime.UtcNow;
        }

    }
}