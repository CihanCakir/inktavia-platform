using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.LoginWithUsername
{
    public class LoginWithUsernameCommand : AizenCommand<UserLoginResponse>
    {
        public string Username { get; set; }
        public string Pin { get; set; }
        public string DeviceId { get; set; }
        public string NotificationToken { get; set; }  

        public LoginWithUsernameCommand(string username, string pin, string deviceId, string notificationToken)
        {
            Username = username;
            Pin = pin;
            DeviceId = deviceId;
            NotificationToken = notificationToken;
        }
    }
}