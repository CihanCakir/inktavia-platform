using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Repository.Context;
using MiniUow;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.RefreshLogin
{
    public class RefreshLoginCommandHandler : IAizenCommandHandler<RefreshLoginCommand, UserLoginResponse>
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IRepository<UserEntity> _userRepository;
        private readonly IRepository<UserLoginTokenEntity> _tokenRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IAizenInfoAccessor _infoAccessor;

        public RefreshLoginCommandHandler(
            IAizenUnitOfWork<IdentityDbContext> unitOfWork,
            IAuthorizationService authorizationService,
            IUserProfileRepository userProfileRepository,
            IAizenInfoAccessor infoAccessor)
        {
            _userRepository = unitOfWork.GetRepository<UserEntity>();
            _tokenRepository = unitOfWork.GetRepository<UserLoginTokenEntity>();
            _authorizationService = authorizationService;
            _userProfileRepository = userProfileRepository;
            _infoAccessor = infoAccessor;
        }

        public bool IsTransactional => true;

        // AppInfo.Code -> RoleContext eşlemesi
        private static WorkshopRoleContext GetRoleContextFromAppInfo(string appCode)
        {
            // Örn. panel kodlarına göre mapping
            // "organizer-app" -> "Organizer"
            // "venue-app"     -> "Venue"
            // "user-app"      -> "Participant"
            // Gerekiyorsa enum’a çevir.
            return appCode switch
            {
                "organizer-app" => WorkshopRoleContext.Organizer,
                "venue-app" => WorkshopRoleContext.VenueOwner,
                "admin-app" => WorkshopRoleContext.Admin,
                _ => WorkshopRoleContext.Participant
            };
        }

        public async Task<UserLoginResponse> Handle(RefreshLoginCommand request, CancellationToken cancellationToken)
        {
            // 0) Panel / AppInfo bazlı access token zorunluluğu
            var appCode = _infoAccessor.ClientInfoAccessor.ClientInfo.ChannelName;
            var currentPanelAccess = _infoAccessor.ClientInfoAccessor.ClientInfo.AuthToken;

            if (string.IsNullOrWhiteSpace(appCode))
                throw new AizenBusinessException(((int)AizenErrorCode.UnknownApplicationContext).ToString());

            // Bu panel/uygulama bağlamından geldiyse access tokenı taşımış olmalı
            if (string.IsNullOrWhiteSpace(currentPanelAccess))
                throw new AizenBusinessException(((int)AizenErrorCode.AccessTokenRequiredForThisPanel).ToString());

            // 1) Cihaz bilgisi (request dolu değilse accessor’dan)
            var deviceIdFromAccessor = _infoAccessor.DeviceInfoAccessor?.DeviceInfo?.DeviceId;
            var deviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? deviceIdFromAccessor : request.DeviceId;

            if (string.IsNullOrWhiteSpace(deviceId))
                throw new AizenBusinessException(((int)AizenErrorCode.DeviceIdRequired).ToString());

            // 2) Token kaydını doğrula (refresh + access + device + revoked=false)
            var userToken = await _tokenRepository.FirstOrDefaultAsync(
                u => u.RefreshToken == request.RefreshToken
                  && u.AccessToken == currentPanelAccess
                  && u.DeviceId == deviceId
                  && u.IsRevoked == false);

            if (userToken is null)
                throw new AizenBusinessException(((int)AizenErrorCode.TokenNotFound).ToString());

            // 3) Süre kontrolü (UTC)
            if (userToken.RefreshTokenExpiration <= DateTime.UtcNow)
                throw new AizenBusinessException(((int)AizenErrorCode.RefreshTokenTimeOut).ToString());

            // 4) Kullanıcıyı getir
            var user = await _userRepository.FirstOrDefaultAsync(x => x.Id == userToken.UserId);
            if (user is null)
                throw new AizenBusinessException(((int)AizenErrorCode.UserNotFound).ToString());

            // 5) Panel/roleContext ve aktif profil kontrolü
            var roleContext = GetRoleContextFromAppInfo(appCode);
            var activeProfile = await _userProfileRepository.GetActiveProfileIdAsync(user.Id, roleContext);
            if (activeProfile is null)
                throw new AizenBusinessException(((int)AizenErrorCode.UserHasNoActiveProfileInThisPanel).ToString());

            // 6) Yeni token üretimi – AuthorizationService’e yönlendir (rotasyon içeride)
            var loginReq = new UserLoginRequest
            {

                // Kimlik / profil
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = activeProfile.FirstName,
                LastName = activeProfile.LastName,
                PhoneNumber = user.PhoneNumber,
                RoleContext = roleContext,
                ActiveProfileId = activeProfile.Id,
                // Cihaz / bildirim / ağ
                DeviceId = deviceId,
            };

            var loginRes = await _authorizationService.CreateLoginToken(loginReq);
            return loginRes;
        }
    }

}