using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.LoginWithUsername
{
    // Credential login (username+PIN) now yields a Keycloak login-ticket HANDOFF (Option 2), not Identity-store
    // tokens — so the AdminPanel BFF (which validates Keycloak RS256) accepts the session and AR2 silent refresh
    // works. Mirrors the admin OTP verify handoff shape.
    public class LoginWithUsernameCommand : AizenCommand<VerifyProviderOtpLoginResponse>
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
