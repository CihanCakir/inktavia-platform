using Aizen.Core.Domain;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities.UserAgreement;
using Aizen.Modules.Payment.Abstraction;
using Microsoft.AspNetCore.Identity;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserEntity : IdentityUser<long>, IAizenEntity
    {
        public LoginType LoginType { get; set; } // Enum olarak tutuluyor

        // 1-to-n profil ilişkisi
        public virtual ICollection<UserProfileEntity> Profiles { get; set; } = new List<UserProfileEntity>();

        // Roller
        public virtual ICollection<UserRoleEntity> UserRoles { get; set; } = new List<UserRoleEntity>();



        public virtual ICollection<UserDeviceEntity>? UserDevices { get; set; }

        public virtual ICollection<UserMessagePermissionEntity>? UserMessagePermissions { get; set; }

        public virtual ICollection<UserPasswordHistoryEntity>? UserPasswordHistories { get; set; }

        //public virtual ICollection<UserTokenEntity>? UserTokens { get; set; }

        public virtual ICollection<UserLoginTokenEntity>? UserLoginTokens { get; set; }
        public virtual ICollection<UserEmailConfirmEntity>? UserEmailConfirms { get; set; }

        public virtual ICollection<UserExternalLoginEntity>? ExternalLogins { get; set; }


        // Audit Fields (BaseEntity üzerinden geliyor)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public long? ActiveProfileId { get; private set; } // Kullanıcının aktif profili

        protected UserEntity() { }

        public static UserEntity CreateLocal(string email, string? phone, string passwordHash, LoginType loginType)
        {
            var u = new UserEntity
            {
                UserName = email,
                Email = email,
                PhoneNumber = phone,
                LoginType = loginType,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow
            };
            // Parola geçmişi ekleme (aktif işaretli)
            u.AppendPasswordHistory(passwordHash, null);
            return u;
        }

        public static UserEntity CreateExternal(string email, LoginType loginType, bool emailVerified)
        {
            return new UserEntity
            {
                UserName = email,
                Email = email,
                EmailConfirmed = emailVerified,
                LoginType = loginType,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow
            };
            // Not: External login’de parola geçmişi eklemiyoruz
        }
        public void SetModified() => ModifiedAt = DateTime.UtcNow;

        // ---- ROL ----
        public void AssignRole(RoleEntity role)
        {
            if (UserRoles.Any(x => x.RoleId == role.Id)) return;
            UserRoles.Add(new UserRoleEntity { User = this, Role = role, UserId = this.Id, RoleId = role.Id });
            SetModified();
        }

        public void RemoveRole(long roleId)
        {
            var link = UserRoles.FirstOrDefault(x => x.RoleId == roleId);
            if (link is null) return;
            UserRoles.Remove(link);
            SetModified();
        }

        // ---- PROFİL ----
        public UserProfileEntity AddProfile(UserProfileEntity profile, bool markAsActive = false)
        {
            profile.User = this;
            profile.UserId = this.Id;
            Profiles.Add(profile);

            if (markAsActive)
                SetActiveProfile(profile.Id);

            SetModified();
            return profile;
        }

        public void SetActiveProfile(long profileId)
        {
            if (!Profiles.Any(p => p.Id == profileId))
                throw new InvalidOperationException("Profile not owned by user.");

            ActiveProfileId = profileId;

            // Parola geçmişi ve device/permission bağlamları bu profile referans verebilir
            SetModified();
        }

        // ---- CİHAZ ----
        public UserDeviceEntity RegisterOrUpdateDevice(
            string deviceId,
            ConsumerDeviceType deviceType,
            string? notificationToken,
            WorkshopRoleContext roleCtx)
        {
            var existing = UserDevices?.FirstOrDefault(d => d.DeviceId == deviceId);
            if (existing is null)
            {
                var dev = UserDeviceEntity.Create(this.Id, deviceId, deviceType, notificationToken, roleCtx, ActiveProfileId);
                (UserDevices ??= new List<UserDeviceEntity>()).Add(dev);
                return dev;
            }
            existing.UpdateLoginInfo(notificationToken, deviceType, roleCtx, ActiveProfileId);
            return existing;
        }

        public void RevokeDevice(string deviceId)
        {
            var existing = UserDevices?.FirstOrDefault(d => d.DeviceId == deviceId);
            existing?.Revoke();
        }

        // ---- MESAJ İZİNLERİ ----
        // using Aizen.Modules.Identity.Domain;
        // using Aizen.Modules.Identity.Domain.Entities;

        public UserMessagePermissionEntity UpsertMessagePermission(
            MessagePermissionTypes permissionType,
            int? contentId,
            bool? value)
        {
            UserMessagePermissions ??= new List<UserMessagePermissionEntity>();

            var perm = UserMessagePermissions
                .FirstOrDefault(p => p.PermissionType == permissionType && p.PermissionContentId == contentId);

            if (perm is null)
            {
                perm = UserMessagePermissionEntity.Create(
                    user: this,
                    type: permissionType,
                    permissionContentId: contentId,
                    value: value,
                    activeProfileId: ActiveProfileId
                );

                UserMessagePermissions.Add(perm);
            }
            else
            {
                // idempotent upsert (davranış methodu entity içinde)
                perm.ApplyUpsert(value, ActiveProfileId);
            }

            SetModified();
            return perm;
        }

        // (opsiyonel) hızlı kısayol: push bildirimi özelinde
        public UserMessagePermissionEntity UpsertPushPermission(bool? allow)
            => UpsertMessagePermission(MessagePermissionTypes.Notification, null, allow);


        // ---- PAROLA TARİHÇESİ ----
        public void AppendPasswordHistory(string passwordHash, long? activeProfileId)
        {
            // Öncekini pasifleştir
            var prev = UserPasswordHistories?.FirstOrDefault(h => h.IsValid);
            if (prev is not null) prev.IsValid = false;

            (UserPasswordHistories ??= new List<UserPasswordHistoryEntity>())
                .Add(new UserPasswordHistoryEntity
                {
                    User = this,
                    UserId = this.Id,
                    PasswordHash = passwordHash,
                    IsValid = true,
                    ActiveProfileId = activeProfileId
                });

            SetModified();
        }

        // ---- SÖZLEŞME ----
        public UserAgreementEntity ApproveAgreement(AgreementEntity agreement)
        {
            var approval = UserAgreementEntity.ApproveAgreement(this.Id, agreement);
            // ilişkisel koleksiyon yoksa repository seviyesinde ekleyeceğiz; burada sadece entity üretimi
            return approval;
        }


        //-- EXTERNAL LOGIN --//
        /// <summary>
        /// Sağlayıcıdan gelen kimliği (sub) kullanıcıya bağlar veya günceller.
        /// Idempotenttir. (Provider, ProviderUserId) tekil olmalıdır.
        /// </summary>
        public UserExternalLoginEntity LinkOrUpdateExternalLogin(
            LoginType provider,
            string providerUserId,
            string? emailAtLinkTime,
            string? scope)
        {
            var current = ExternalLogins
                .FirstOrDefault(x => x.Provider == provider && x.ProviderUserId == providerUserId);

            if (current is null)
            {
                // Aynı provider için eski farklı sub varsa (nadir) onu güncelle/normalize etmek isteyebilirsin
                current = UserExternalLoginEntity.Create(this.Id, provider, providerUserId, emailAtLinkTime, scope);
                ExternalLogins.Add(current);
            }
            else
            {
                current.Update(providerUserId, emailAtLinkTime, scope);
            }

            // Son kullanılan giriş tipini güncel tut
            this.LoginType = provider == LoginType.Apple ? LoginType.Apple : LoginType.Google;
            SetModified();
            return current;
        }

        /// <summary>
        /// Sağlayıcının doğrulanmış e-postası varsa kullanıcıya uygular.
        /// </summary>
        public void ApplyEmailVerificationFromProvider(bool emailVerified, string? emailFromProvider)
        {
            if (!string.IsNullOrWhiteSpace(emailFromProvider) && !string.Equals(Email, emailFromProvider, StringComparison.OrdinalIgnoreCase))
            {
                // Yerelde e-posta boş/yanlış ise sağlayıcı e-postasıyla eşitle (iş kuralına göre)
                Email = emailFromProvider;
                UserName = emailFromProvider;
            }

            if (emailVerified && !EmailConfirmed)
                EmailConfirmed = true;

            SetModified();
        }

        /// <summary>
        /// Harici giriş sonrası isim/telefon gibi basit alanları günceller (boşsa doldur).
        /// </summary>
        public void ApplyExternalProfileHints(string? fullName, string? phone = null)
        {
            // İsim profilde tutuluyorsa (FullName), aktif profile uygula; yoksa fallback olarak UserName alanlarını güncellemek de mümkün.
            var active = Profiles.FirstOrDefault(p => p.Id == ActiveProfileId) ?? Profiles.FirstOrDefault(p => p.IsActive);
            if (active is not null)
            {
                if (string.IsNullOrWhiteSpace(active.FirstName) && !string.IsNullOrWhiteSpace(fullName))
                    active.ChangeName(fullName, active.LastName);
            }

            SetModified();
        }

        /// <summary>
        /// Kullanıcının Participant rolüne ve aktif Participant profiline sahip olmasını garanti eder.
        /// Yoksa oluşturur ve aktifler.
        /// </summary>
        public UserProfileEntity EnsureParticipantProfileAndRole(
            RoleEntity participantRole,
            string? nameFromProvider = null,
            TaxpayerType taxpayerType = TaxpayerType.Individual,
            string? gender = null,
            DateTime? birthDate = null,
            string? profilePhotoUrl = null)
        {
            // 1) Rolü garanti et
            AssignRole(participantRole);

            // 2) Aktif profil ya da var olan Participant profili
            var active = Profiles.FirstOrDefault(p => p.Id == ActiveProfileId);
            if (active is null)
                active = Profiles.FirstOrDefault(p => p.RoleContext == WorkshopRoleContext.Participant);

            // 3) Yoksa oluştur
            if (active is null)
            {
                var (first, last) = SplitName(nameFromProvider);

                // Factory userId istiyor; yeni kullanıcıda 0 olabilir. AddProfile zaten UserId'yi overwrite ediyor.
                active = UserProfileEntity.Create(
                    userId: this.Id,
                    firstName: first,
                    lastName: last,
                    taxpayerType: taxpayerType,
                    gender: gender,
                    birthDate: birthDate,
                    bio: null,
                    profilePhotoUrl: profilePhotoUrl
                );
                active.RoleContext = WorkshopRoleContext.Participant;

                AddProfile(active, markAsActive: true); // ActiveProfileId set edilir
            }
            else
            {
                // Aktif profili işaretle
                SetActiveProfile(active.Id);

                // Boş/eksik alanları sağlayıcı ipuçlarıyla tamamla (ad/soyad, cinsiyet, doğum tarihi, foto)
                var (first, last) = SplitName(nameFromProvider);

                if (string.IsNullOrWhiteSpace(active.FirstName) && !string.IsNullOrWhiteSpace(first))
                    active.ChangeName(first, string.IsNullOrWhiteSpace(active.LastName) ? (last ?? active.LastName) : active.LastName);

                if (!string.IsNullOrWhiteSpace(last) && string.IsNullOrWhiteSpace(active.LastName))
                    active.ChangeName(active.FirstName, last!);

                if (!string.IsNullOrWhiteSpace(gender))
                    active.UpdateGender(gender);

                if (birthDate.HasValue)
                    active.UpdateBirthDate(birthDate);

                if (!string.IsNullOrWhiteSpace(profilePhotoUrl))
                    active.UpdateProfilePhoto(profilePhotoUrl);
            }

            SetModified();
            return active;
        }


        public void ApplyExternalProfileHints(string? nameFromProvider, string? gender = null, DateTime? birthDate = null, string? profilePhotoUrl = null)
        {
            var profile = Profiles.FirstOrDefault(p => p.Id == ActiveProfileId)
                       ?? Profiles.FirstOrDefault(p => p.RoleContext == WorkshopRoleContext.Participant);

            if (profile is null)
                return;

            var (first, last) = SplitName(nameFromProvider);

            if (!string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(profile.FirstName))
                profile.ChangeName(first, string.IsNullOrWhiteSpace(profile.LastName) ? (last ?? profile.LastName) : profile.LastName);

            if (!string.IsNullOrWhiteSpace(last) && string.IsNullOrWhiteSpace(profile.LastName))
                profile.ChangeName(profile.FirstName, last!);

            if (!string.IsNullOrWhiteSpace(gender))
                profile.UpdateGender(gender);

            if (birthDate.HasValue)
                profile.UpdateBirthDate(birthDate);

            if (!string.IsNullOrWhiteSpace(profilePhotoUrl))
                profile.UpdateProfilePhoto(profilePhotoUrl);

            SetModified();
        }

        private static (string first, string? last) SplitName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return ("", null);

            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return (parts[0], null);

            var first = parts[0];
            var last = string.Join(' ', parts.Skip(1));
            return (first, last);
        }

    }
}