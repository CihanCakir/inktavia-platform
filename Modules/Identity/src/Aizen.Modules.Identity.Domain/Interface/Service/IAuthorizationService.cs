using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Modules.Identity.Domain.Interface
{
    public interface IAuthorizationService
    {
        public Task<UserLoginResponse> CreateLoginToken(UserLoginRequest request);
        public Task CheckDeviceHasBeenApproval(UserLoginRequest request);
        Task<SendOtpDto> SendOtpAsync(string phoneNumber);
        Task<SendOtpDto> SendOtpAsync(string phoneNumber, OtherPersonOtpRequest smsDto);
        Task<OtpVerificationResultDto> CheckOtpAsync(string phoneNumber, int otp, string validationGuid);
    }
}