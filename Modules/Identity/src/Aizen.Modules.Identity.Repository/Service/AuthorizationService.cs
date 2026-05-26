using Aizen.Core.Api.Middleware;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Enum;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.Extensions.Configuration;
using MiniUow;

namespace Aizen.Modules.Identity.Repository.Identity.Service
{
    public class AuthorizationService : IAuthorizationService
    {

        private readonly int _generalAgreementId;

        private readonly IInktaviaTokenService _tokenHelper;
        private readonly IUserLoginTokenRepository _userLoginTokenRepository;
        private readonly IUserDeviceRepository _userDeviceRepository;
        private readonly IAgreementRepository _agreementRepository;
        private readonly IConfiguration _configuration;
        private readonly IAizenInfoAccessor _aizenInfoAccessor;
        private readonly IRepository<UserValidationEntity> _userValidationRepository;

        public AuthorizationService(
            IInktaviaTokenService tokenHelper,
            IUserLoginTokenRepository userLoginTokenRepository,
            IUserDeviceRepository userDeviceRepository,
            IAgreementRepository agreementRepository,
            IConfiguration configuration,
            IAizenInfoAccessor aizenInfoAccessor,
            IAizenUnitOfWork<IdentityDbContext> userValidationRepository
        )
        {
            _userValidationRepository = userValidationRepository.GetRepository<UserValidationEntity>() ?? throw new ArgumentNullException(nameof(userValidationRepository));
            _aizenInfoAccessor = aizenInfoAccessor ?? throw new ArgumentNullException(nameof(aizenInfoAccessor));
            _configuration = configuration;
            _tokenHelper = tokenHelper;
            _userLoginTokenRepository = userLoginTokenRepository;
            _userDeviceRepository = userDeviceRepository;
            _agreementRepository = agreementRepository;
            _generalAgreementId = _configuration.GetSection("Agreements").GetValue<int>("General");

        }


        public async Task CheckDeviceHasBeenApproval(UserLoginRequest request)
        {
            var today = DateTime.UtcNow.Date;

            // 1. Cihaz bugün zaten engellenmiş mi?
            var isBlocked = await _userDeviceRepository.IsDeviceBlockedTodayAsync(request.DeviceId, today);
            if (isBlocked)
                throw new AizenBusinessException((int)AizenErrorCode.DeviceAlreadyBlocked);

            // 2. Bu cihazdan kaç farklı kullanıcı bugün giriş yaptı?
            var loginUserCount = await _userDeviceRepository.GetTodayLoginUserCountByDeviceIdAsync(request.DeviceId, today);
            if (loginUserCount >= 3)
            {
                // 3. Engelleme işlemi
                var blockEntity = UserDeviceBlockEntity.Create(
                    deviceName: _aizenInfoAccessor.DeviceInfoAccessor.DeviceInfo.Brand.ToString() ?? "Unknown",
                    deviceId: request.DeviceId,
                    ipAddress: _aizenInfoAccessor.DeviceInfoAccessor.DeviceInfo.IpAddress ?? "Unknown",
                    deviceTypeId: int.TryParse(_aizenInfoAccessor.AppInfoAccessor.AppInfo.Name.ToString(), out var pt) ? pt : null
                );

                await _userDeviceRepository.AddDeviceBlockAsync(blockEntity);

                throw new AizenBusinessException((int)AizenErrorCode.DeviceBlockedDueToExcessLoginAttempt)
                {
                    IsRollback = false
                };
            }
        }

        public async Task<UserLoginResponse> CreateLoginToken(UserLoginRequest request)
        {
            // 1. Cihaz güvenlik kontrolü (Aşırı deneme vs.)
            await CheckDeviceHasBeenApproval(request);

            // 2. Token oluştur
            var accessToken = _tokenHelper.GenerateToken(request.UserId, request.UserName ?? request.Email, roles: new List<string>(request.Roles ?? new List<string>()) { request.RoleContext.ToString() });

            var refreshToken = _tokenHelper.GenerateRefreshToken();
            var accessTokenExpiration = _tokenHelper.GetAccessTokenExpiration();
            var refreshTokenExpiration = _tokenHelper.GetRefreshTokenExpiration();

            // 3. Giriş yapan kullanıcı bilgileri
            var userInfo = new UserInfo(
                request.UserId,
                request.Email,
                request.NationalityId,
                request.FirstName,
                request.LastName,
                request.PhoneNumber
            );

            // 4. Giriş yapılan token bilgileri
            var tokenInfo = new TokenInfo(
                accessToken.AccessToken,
                accessTokenExpiration,
                refreshToken,
                refreshTokenExpiration
            );

            // 5. Geçerli sözleşme kontrolü
            var agreementId = await _agreementRepository.HasUserApprovedAgreementAsync(_generalAgreementId, request.UserId);
            var agreementInfo = new AgreementInfo
            {
                AgreementId = agreementId,
                AgreementApproved = agreementId > 0
            };

            // 6. Token revocation işlemleri (en fazla 2 aktif token)
            await _userLoginTokenRepository.RevokeOldTokensAsync(request.UserId);


            // 7. Yeni token kaydı oluştur
            var newToken = UserLoginTokenEntity.Create(
                userId: request.UserId,
                accessToken.AccessToken,
                refreshToken,
                accessTokenExpiration,
                refreshTokenExpiration,
                deviceId: request.DeviceId,
                roleContext: request.RoleContext,
                activeProfileId: request.UserId
            );

            await _userLoginTokenRepository.AddAsync(newToken);

            // 8. Cihaz bilgisi güncelle (UserDeviceRepository)
            await _userDeviceRepository.AddOrUpdateDeviceAsync(
                request.UserId,
                request.DeviceId,
                string.IsNullOrWhiteSpace(request.NotificationToken) ? "EmptyNotificationToken" : request.NotificationToken,
                request.DeviceType,
                request.RoleContext,
                request.UserId
            );

            // 9. Response oluştur
            return new UserLoginResponse
            {
                Token = tokenInfo,
                Profile = userInfo,
                Agreement = agreementInfo
            };
        }

        public async Task<SendOtpDto> SendOtpAsync(string phoneNumber)
        {
            var validationCode = new Random().Next(100000, 999999);
            var now = DateTime.UtcNow;
            var guid = Guid.NewGuid().ToString();

            var validation = UserValidationEntity.Create(
                UserValidationType.Login,
                UserValidationMethodType.Sms,
                phoneNumber,
                validationCode,
                guid,
                now.AddMinutes(5),
                 0, // TODO: Bu değer ne anlama geliyor? Gerekli mi?
                userProfileId: 0 // login işlemi için profile gerek yok
            );

            await _userValidationRepository.AddAsync(validation);

            return new SendOtpDto
            {
                PhoneNumber = phoneNumber,
                ValidationGuid = guid,
                ExpiredDateTime = validation.ExpiredDateTime,
                ExpireCounter = (int)(validation.ExpiredDateTime - DateTime.UtcNow).TotalSeconds
            };
        }

        public async Task<SendOtpDto> SendOtpAsync(string phoneNumber, OtherPersonOtpRequest smsDto)
        {
            var validationCode = new Random().Next(100000, 999999);
            var now = DateTime.UtcNow;
            var guid = Guid.NewGuid().ToString();

            var validation = UserValidationEntity.Create(
                UserValidationType.Login,
                UserValidationMethodType.Sms,
                phoneNumber,
                validationCode,
                guid,
                now.AddMinutes(5),
                0, //TODO: Bu değer ne anlama geliyor? Gerekli mi?
                userProfileId: 0,
                smsDto.ContextType.ToString()
            );

            await _userValidationRepository.AddAsync(validation);

            return new SendOtpDto
            {
                PhoneNumber = phoneNumber,
                ValidationGuid = guid,
                ExpiredDateTime = validation.ExpiredDateTime,
                ExpireCounter = (int)(validation.ExpiredDateTime - DateTime.UtcNow).TotalSeconds
            };
        }

        public async Task<OtpVerificationResultDto> CheckOtpAsync(string phoneNumber, int otp, string validationGuid)
        {
            var validation = await _userValidationRepository.FirstOrDefaultAsync(x =>
                x.ValidationReferance == phoneNumber &&
                x.ValdationCode == otp &&
                x.ValidationGuid == validationGuid &&
                x.ExpiredDateTime >= DateTime.UtcNow &&
                x.State == 1);

            if (validation == null)
                throw new AizenBusinessException((int)AizenErrorCode.OtpCodeIsNotValid);

            validation.MarkAsUsed();
            _userValidationRepository.Update(validation);

            return new OtpVerificationResultDto { IsVerified = true };
        }

    }

}