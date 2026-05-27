using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Repository.Context;
using MiniUow;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.LoginWithOtp
{
    public class LoginWithOtpCommandHandler : AizenCommandHandler<LoginWithOtpCommand, UserLoginResponse>
    {
        private readonly IRepository<UserValidationEntity> _userValidationRepository;
        private readonly IRepository<UserEntity> _userRepository;
        private readonly IRepository<UserDeviceEntity> _deviceRepository;
        private readonly IAuthorizationService _sharedService;
        private readonly IUserProfileRepository _userProfileRepository;

        public LoginWithOtpCommandHandler(
            IAizenUnitOfWork<IdentityDbContext> unitOfWork,
            IAuthorizationService sharedService,
            IUserProfileRepository userProfileRepository)
        {
            _userValidationRepository = unitOfWork.GetRepository<UserValidationEntity>();
            _userRepository = unitOfWork.GetRepository<UserEntity>();
            _deviceRepository = unitOfWork.GetRepository<UserDeviceEntity>();
            _sharedService = sharedService;
            _userProfileRepository = userProfileRepository;
        }

        public override async Task<UserLoginResponse?> Handle(LoginWithOtpCommand request, CancellationToken cancellationToken)
        {
            var userValidation = await _userValidationRepository.FirstOrDefaultAsync(
                x => x.ValidationReferance == request.PhoneNumber &&
                     x.ValdationCode == request.Otp &&
                     x.ValidationGuid == request.ValidationGuid &&
                     x.ExpiredDateTime >= DateTime.UtcNow &&
                     x.State == 1);

            if (userValidation is null)
                throw new AizenBusinessException(((int)AizenErrorCode.OtpWrong).ToString());

            userValidation.State = 2;
            _userValidationRepository.Update(userValidation);

            var user = await _userRepository.FirstOrDefaultAsync(x =>
                x.PhoneNumber == request.PhoneNumber && x.IsDeleted == false);
            if (user is null)
                throw new AizenBusinessException(((int)AizenErrorCode.PassiveUser).ToString());

            var profile = await _userProfileRepository.GetProfileByIdAsync(userValidation.UserProfileId);

            if (profile is null)
                throw new AizenBusinessException(((int)AizenErrorCode.UserHasNoActiveProfileInThisPanel).ToString());

            var device = await _deviceRepository.FirstOrDefaultAsync(x =>
                x.DeviceId == request.DeviceId && x.UserId == user.Id);

            if (device is null)
            {
                await _deviceRepository.AddAsync(new UserDeviceEntity
                {
                    IsActive = true,
                    DeviceId = request.DeviceId,
                    NotificationToken = request.NotificationToken,
                    UserId = user.Id
                });
            }

            var tokenResult = await _sharedService.CreateLoginToken(new UserLoginRequest
            {
                DeviceId = request.DeviceId,
                Email = user.Email,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                PhoneNumber = user.PhoneNumber,
                UserId = user.Id,
                NotificationToken = request.NotificationToken,
            });



            return tokenResult;
        }
    }
}
