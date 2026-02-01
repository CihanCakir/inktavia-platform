using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.LoginWithOtp
{
    public class LoginWithOtpCommand : AizenCommand<UserLoginResponse>
    {
        public string PhoneNumber { get; }
        public int Otp { get; }
        public string ValidationGuid { get; }
        public string DeviceId { get; }
        public string NotificationToken { get; }

        public LoginWithOtpCommand(string phoneNumber, int otp, string validationGuid, string deviceId, string notificationToken)
        {
            PhoneNumber = phoneNumber;
            Otp = otp;
            ValidationGuid = validationGuid;
            DeviceId = deviceId;
            NotificationToken = notificationToken;
        }
    }
}