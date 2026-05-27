using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command
{
   public class LoginWithPhoneNumberCommand : AizenCommand<UserLoginResponse>
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