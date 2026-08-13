using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command
{
    // Credential login (phone+password) now yields a Keycloak login-ticket HANDOFF (Option 2), not Identity-store
    // tokens — same shape as the admin OTP verify handoff. See LoginWithUsernameCommand for the rationale.
    public class LoginWithPhoneNumberCommand : AizenCommand<VerifyProviderOtpLoginResponse>
    {
        /// <summary>
        /// Kullanıcının kayıtlı telefon numarası (örn: +905xx...)
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Kullanıcının şifresi
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Cihaza ait unique kimlik bilgisi
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// Push notification için notification token bilgisi (isteğe bağlı)
        /// </summary>
        public string NotificationToken { get; set; } = string.Empty;

        public LoginWithPhoneNumberCommand(string phoneNumber, string password, string deviceId, string notificationToken)
        {
            PhoneNumber = phoneNumber;
            Password = password;
            DeviceId = deviceId;
            NotificationToken = notificationToken;
        }
    }
}
