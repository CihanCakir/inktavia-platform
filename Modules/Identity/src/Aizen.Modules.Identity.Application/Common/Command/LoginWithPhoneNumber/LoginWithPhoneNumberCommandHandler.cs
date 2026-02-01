using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Microsoft.AspNetCore.Identity;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command
{
    public class LoginWithPhoneNumberCommandHandler : AizenCommandHandler<LoginWithPhoneNumberCommand, UserLoginResponse>
    {
        private const int ValidAttemptNumber = 2;

        private readonly PasswordValidator<UserEntity> _passwordValidator;
        private readonly UserManager<UserEntity> _userManager;
        private readonly SignInManager<UserEntity> _signInManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly IUserRepository _userRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IUserDeviceRepository _userDeviceRepository;
        private readonly IAizenInfoAccessor _infoAccessor;


        public LoginWithPhoneNumberCommandHandler(
            PasswordValidator<UserEntity> passwordValidator,
            UserManager<UserEntity> userManager,
            SignInManager<UserEntity> signInManager,
            IUserRepository userRepository,
            IUserProfileRepository userProfileRepository,
            IUserDeviceRepository userDeviceRepository,
            IAuthorizationService authorizationService,
            IAizenInfoAccessor infoAccessor)
        {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _passwordValidator = passwordValidator;
            _userManager = userManager;
            _signInManager = signInManager;
            _userRepository = userRepository;
            _userProfileRepository = userProfileRepository;
            _userDeviceRepository = userDeviceRepository;
            _infoAccessor = infoAccessor;
        }

        public override async Task<UserLoginResponse?> Handle(LoginWithPhoneNumberCommand request, CancellationToken cancellationToken)
        {
            // 1. Kullanıcıyı getir
            var user = await _userRepository.GetUserByPhoneNumber(request.PhoneNumber, disableTracking: false)
                        ?? throw new AizenBusinessException(((int)AizenErrorCode.UserNotFound).ToString());


            if (!user.PhoneNumberConfirmed)
                throw new AizenBusinessException(((int)AizenErrorCode.PhoneNotConfirmed).ToString());

            // 2. Bloklu mu kontrol et
            if (await _userManager.IsLockedOutAsync(user))
                throw new AizenBusinessException(((int)AizenErrorCode.CurrentDeviceHasBeenLockup).ToString());

            // 3. Giriş denemesi
            var signIn = await _signInManager.PasswordSignInAsync(user.PhoneNumber, request.Password, true, true);

            if (!signIn.Succeeded)
            {
                if (user.AccessFailedCount >= ValidAttemptNumber)
                    await _userRepository.BlockUser(user);

                await _userRepository.FailLogin(user);
                throw new AizenBusinessException(((int)AizenErrorCode.LoginFailedForPasswordBlockedUser).ToString());
            }

            // 1. Kullanıcının profilleri var mı?
            var profiles = await _userProfileRepository.GetProfileByIdAsync(user.Id);
            if (profiles == null)
                throw new AizenBusinessException(((int)AizenErrorCode.  UserHasNoActiveProfileInThisPanel).ToString());

            // 4. Profil kontrolü (aktif profil var mı?)
            var appCode =  _infoAccessor.AppInfoAccessor.AppInfo.Code;
            var roleContext = appCode switch
            {
                "ORGANIZER" => WorkshopRoleContext.Organizer,
                "VENUE" => WorkshopRoleContext.VenueOwner,
                _ => WorkshopRoleContext.Participant
            };

            var hasProfile = await _userProfileRepository.HasProfileForContextAsync(user.Id, roleContext);
            if (!hasProfile)
                throw new AizenBusinessException(((int)AizenErrorCode.UserHasNoActiveProfileInThisPanel).ToString());

            var activeProfileId = await _userProfileRepository.GetActiveProfileIdAsync(user.Id, roleContext);

            // 5. Token oluştur
            var result = await _authorizationService.CreateLoginToken(new UserLoginRequest
            {
                DeviceId = request.DeviceId,
                Email = user.Email,
                FirstName = profiles.FirstName,
                NationalityId = profiles.NationalityId,
                PhoneNumber = user.PhoneNumber,
                LastName = profiles.LastName,
                UserId = user.Id,
                NotificationToken = request.NotificationToken,
                RoleContext = roleContext,
                ActiveProfileId = profiles.Id
            });


            return result;
        }
    }
}
