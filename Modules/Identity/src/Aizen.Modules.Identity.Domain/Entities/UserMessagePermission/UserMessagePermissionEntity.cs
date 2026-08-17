using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.Domain;
using Aizen.Modules.Identity.Abstraction;

namespace Aizen.Modules.Identity.Domain.Entities
{
    // Domain/Identity/Entities/UserMessagePermissionEntity.cs
    public class UserMessagePermissionEntity : AizenEntityWithAudit
    {
        public long UserId { get; private set; }
        public required virtual UserEntity User { get; set; }

        // int -> enum
        public MessagePermissionTypes PermissionType { get; private set; }

        // İçerik kapsamı (ör. kampanyaId, konuId). Null ise genel izin.
        public int? PermissionContentId { get; private set; }

        // true = izinli, false = red, null = belirlenmemiş
        public bool? PermissionValue { get; private set; }

        public bool IsActive { get; private set; } = true;

        // TR İYS senkronizasyon durumu
        public bool IYSStatus { get; private set; } = false;

        // Bağlam: işlem anındaki aktif profil
        public long? ActiveProfileId { get; private set; }
        public virtual UserProfileEntity? ActiveProfile { get; private set; }

        // EF Core lazy-loading proxies (Castle DynamicProxy) subclass the entity, so the parameterless ctor must be
        // at least protected. A private one makes every query that materializes this type fail at runtime.
        protected UserMessagePermissionEntity() { }

        // -------- Factory (idempotent amaçlı anahtar: User + Type + Content) --------
        public static UserMessagePermissionEntity Create(
            UserEntity user,
            MessagePermissionTypes type,
            int? permissionContentId,
            bool? value,
            long? activeProfileId)
        {
            return new UserMessagePermissionEntity
            {
                User = user,
                UserId = user.Id,
                PermissionType = type,
                PermissionContentId = permissionContentId,
                PermissionValue = value,
                ActiveProfileId = activeProfileId,
                IYSStatus = false,
                IsActive = true,
                CreateDate = DateTime.UtcNow
            };
        }

        // -------- Invariant / Key eşleştirme --------
        public bool IsSameKey(long userId, MessagePermissionTypes type, int? contentId) =>
            UserId == userId && PermissionType == type && PermissionContentId == contentId;

        // -------- Davranışlar --------

        /// <summary>İzni açık (opt-in) yapar.</summary>
        public void Allow(long? activeProfileId)
        {
            PermissionValue = true;
            IsActive = true;
            ActiveProfileId = activeProfileId;
            Touch();
            // İYS’de gerekli kanallar için işaret temizlenir; yeni durum push edilecek.
            IYSStatus = false;
        }

        /// <summary>İzni kapalı (opt-out) yapar.</summary>
        public void Deny(long? activeProfileId)
        {
            PermissionValue = false;
            IsActive = true;
            ActiveProfileId = activeProfileId;
            Touch();
            IYSStatus = false;
        }

        /// <summary>İzni belirsiz hâle getirir (null).</summary>
        public void Unset(long? activeProfileId)
        {
            PermissionValue = null;
            IsActive = true;
            ActiveProfileId = activeProfileId;
            Touch();
            IYSStatus = false;
        }

        /// <summary>Aktif/pasif durumunu değiştirir.</summary>
        public void ToggleActivity()
        {
            IsActive = !IsActive;
            Touch();
        }

        /// <summary>İYS senkronizasyonu tamamlandı işareti.</summary>
        public void MarkIysSynced()
        {
            IYSStatus = true;
            Touch();
        }

        /// <summary>Upsert senaryosu için gelen değerleri idempotent şekilde uygular.</summary>
        public void ApplyUpsert(bool? value, long? activeProfileId)
        {
            // Aynı değer tekrar gelirse tarih vb. yine güncellenebilir (audit için)
            PermissionValue = value;
            IsActive = true;
            ActiveProfileId = activeProfileId;
            IYSStatus = false; // yeni durum sonrası tekrar sync gerekebilir
            Touch();
        }

        private void Touch() => ModifyDate = DateTime.UtcNow;
    }

}