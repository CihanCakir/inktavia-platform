using Aizen.Core.Api.Middleware;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Payment.Abstraction;
using Microsoft.AspNetCore.Identity;

namespace Aizen.Modules.Identity.Repository.Identity.Service
{
    public sealed class ConsumerRegistrationDomainService : IConsumerRegistrationDomainService
    {
        private readonly UserManager<UserEntity> _userManager;
        private readonly RoleManager<RoleEntity> _roleManager;
        private readonly IAgreementRepository _agreementRepo;
        private readonly IUserProfileRepository _profileRepo;
        private readonly IUserDeviceRepository _deviceRepo;
        private readonly IUserRepository _userRepo; // phone lookup & misc.
        private readonly IUserMessagePermissionRepository _messagePermissionRepository;

        public ConsumerRegistrationDomainService(
            UserManager<UserEntity> userManager,
            RoleManager<RoleEntity> roleManager,
            IAgreementRepository agreementRepo,
            IUserProfileRepository profileRepo,
            IUserDeviceRepository deviceRepo,
            IUserRepository userRepo,
            IUserMessagePermissionRepository messagePermissionRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _agreementRepo = agreementRepo;
            _profileRepo = profileRepo;
            _deviceRepo = deviceRepo;
            _userRepo = userRepo;
            _messagePermissionRepository = messagePermissionRepository;
        }

        public async Task<(UserEntity user, UserProfileEntity profile)> RegisterOrAttachAsync(
            RegisterParticipantDomainModel m, CancellationToken ct)
        {
            if (!m.KvkkAccepted)
                throw new AizenBusinessException(((int)AizenErrorCode.RequiredAgreementsMissing).ToString());

            // 1) Kullanıcıyı bul (email öncelikli, sonra phone)
            UserEntity? user = null;
            if (!string.IsNullOrWhiteSpace(m.Email))
                user = await _userManager.FindByEmailAsync(m.Email);

            if (user is null && !string.IsNullOrWhiteSpace(m.Phone))
                user = await _userRepo.CheckUserByPhoneNumber(m.Phone!, disableTracking: false);

            var isNew = user is null;

            // 2) Yoksa oluştur; varsa parola yoksa set et
            if (isNew)
            {
                user = UserEntity.CreateLocal(
                    email: m.Email,
                    phone: m.Phone,
                    passwordHash: string.Empty,
                    loginType: LoginType.Email);

                var createRes = await _userManager.CreateAsync(user, m.Password);
                if (!createRes.Succeeded)
                    throw new AizenBusinessException(((int)AizenErrorCode.UserCreationFailed).ToString());

                // Parola geçmişi: sizin PasswordHistory repo’nuz ayrı ise orada ekleniyor olabilir.
                // (Eğer farklı bir repo ile eklemek isterseniz burada çağırın.)
            }
            else if (string.IsNullOrWhiteSpace(user!.PasswordHash))
            {
                var addPwd = await _userManager.AddPasswordAsync(user, m.Password);
                if (!addPwd.Succeeded)
                    throw new AizenBusinessException(((int)AizenErrorCode.PasswordSetFailed).ToString());
            }
            else
            {
                // Eşleşmeyen email/phone ile çakışma kontrolü (opsiyonel, güvenlik için iyi olur)
                if (!string.IsNullOrWhiteSpace(m.Email) && !string.Equals(user.Email, m.Email, StringComparison.OrdinalIgnoreCase))
                    throw new AizenBusinessException(((int)AizenErrorCode.EmailConflictWithExistingUser).ToString());
                if (!string.IsNullOrWhiteSpace(m.Phone) && user.PhoneNumber != m.Phone)
                    throw new AizenBusinessException(((int)AizenErrorCode.PhoneConflictWithExistingUser).ToString());
            }

            // 3) Her tipten bir — Consumer profili var mı?
            var consumerCtx = WorkshopRoleContext.Participant;
            var hasConsumer = await _profileRepo.HasProfileForContextAsync(user!.Id, consumerCtx);
            if (hasConsumer)
                throw new AizenBusinessException(((int)AizenErrorCode.UserAlreadyHasProfileOfThisType).ToString());

            // 4) Rol ata (Consumer)
            var consumerRole = await _roleManager.FindByNameAsync(RoleNames.Consumer);
            if (consumerRole is null)
                throw new AizenBusinessException(((int)AizenErrorCode.RoleNotFound).ToString());

            var isInRole = await _userManager.IsInRoleAsync(user, consumerRole.Name!);
            if (!isInRole)
            {
                var roleRes = await _userManager.AddToRoleAsync(user, consumerRole.Name!);
                if (!roleRes.Succeeded)
                    throw new AizenBusinessException(((int)AizenErrorCode.RoleAssignFailed).ToString());
            }

            // 5) Profil oluştur + aktif yap
            var profile = UserProfileEntity.Create(
                userId: user.Id,
                firstName: m.FirstName,
                lastName: m.LastName,
                taxpayerType: TaxpayerType.Individual);
            profile.RoleContext = consumerCtx;

            // Entity seviyesinde bağla (markAsActive: false — profile.Id henüz 0, FK ihlali yaratır)
            user.AddProfile(profile, markAsActive: false);

            // Persist: profile.Id burada DB tarafından üretilir
            await _profileRepo.AddProfileAsync(profile);
            await _userManager.UpdateAsync(user);

            // Gerçek ID atandıktan sonra aktif profili işaretle
            user.SetActiveProfile(profile.Id);
            await _userManager.UpdateAsync(user);

            // 6) Sözleşmeler (sadece onaylanmamış zorunlu olanları topla → onayla)
            var unapproved = await _agreementRepo.GetUnapprovedAgreementsAsync(user.Id, m.RequiredAgreementTypes);
            foreach (var ag in unapproved)
                _ = await _agreementRepo.ApproveAgreementAsync(user.Id, ag);

            // 7) Cihaz (varsa)
            if (!string.IsNullOrWhiteSpace(m.DeviceId) && m.DeviceType is not null)
            {
                await _deviceRepo.AddOrUpdateDeviceAsync(
                    userId: user.Id,
                    deviceId: m.DeviceId!,
                    notificationToken: m.NotificationToken,
                    deviceType: m.DeviceType.Value,
                    roleContext: m.RoleContext,                // genelde Participant
                    activeProfileId: user.ActiveProfileId      // yeni aktif profil
                );
            }

            // 8) (İsteğe bağlı) Mesaj izni: sizde şu an MessagePermission repo interface’i yok.
            // Varsa burada notificationToken'a göre true/false upsert edebilirsiniz.

            var wantsPush = !string.IsNullOrWhiteSpace(m.NotificationToken);
            var perm = UserMessagePermissionEntity.Create(
                user: user,
                type: MessagePermissionTypes.Notification,
                permissionContentId: null,           // genel push izni
                value: wantsPush,                    // token varsa true
                activeProfileId: user.ActiveProfileId
            );
            await _messagePermissionRepository.AddOrUpdateAsync(perm, ct);

            return (user, profile);
        }

     
    }
}