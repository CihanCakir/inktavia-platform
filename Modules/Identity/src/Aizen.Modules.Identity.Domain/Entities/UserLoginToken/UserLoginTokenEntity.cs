using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.Domain;
using Aizen.Modules.Identity.Abstraction;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserLoginTokenEntity : AizenEntityWithAudit
    {
        public long UserId { get; set; }
        public virtual UserEntity User { get; set; } = null!;

        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime AccessTokenExpiration { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }

        public bool IsRevoked { get; set; } = false;

        public string DeviceId { get; set; } = null!;
        public WorkshopRoleContext RoleContext { get; set; }
        public long? ActiveProfileId { get; set; }

        // -------------------------
        // ✅ Factory Method
        // -------------------------
        public static UserLoginTokenEntity Create(
            long userId,
            string accessToken,
            string refreshToken,
            DateTime accessTokenExpiration,
            DateTime refreshTokenExpiration,
            string deviceId,
            WorkshopRoleContext roleContext,
            long? activeProfileId)
        {
            return new UserLoginTokenEntity
            {
                UserId = userId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiration = accessTokenExpiration,
                RefreshTokenExpiration = refreshTokenExpiration,
                IsRevoked = false,
                DeviceId = deviceId,
                RoleContext = roleContext,
                ActiveProfileId = activeProfileId,
                CreateDate = DateTime.UtcNow,
                ModifyDate = DateTime.UtcNow
            };
        }

        // -------------------------
        // 🔁 Token Yenileme
        // -------------------------
        public void RefreshTokens(
            string newAccessToken,
            string newRefreshToken,
            DateTime newAccessTokenExpiration,
            DateTime newRefreshTokenExpiration)
        {
            AccessToken = newAccessToken;
            RefreshToken = newRefreshToken;
            AccessTokenExpiration = newAccessTokenExpiration;
            RefreshTokenExpiration = newRefreshTokenExpiration;
            ModifyDate = DateTime.UtcNow;
            IsRevoked = false;
        }

        // -------------------------
        // ❌ Revoke Et
        // -------------------------
        public void Revoke()
        {
            IsRevoked = true;
            ModifyDate = DateTime.UtcNow;
        }

        // -------------------------
        // ✅ Token Aktif mi?
        // -------------------------
        public bool IsValid()
        {
            return !IsRevoked && AccessTokenExpiration > DateTime.UtcNow;
        }

        // -------------------------
        // 📱 Cihaz Doğrulaması (isteğe bağlı kontrol)
        // -------------------------
        public bool IsFromSameDevice(string currentDeviceId)
        {
            return string.Equals(DeviceId, currentDeviceId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
