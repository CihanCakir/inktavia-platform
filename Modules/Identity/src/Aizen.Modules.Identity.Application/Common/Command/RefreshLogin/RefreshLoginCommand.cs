using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Modules.InktaviaStore.Application.Identity
{
    public class RefreshLoginCommand : AizenCommand<UserLoginResponse>
    {
        public string RefreshToken { get; set; }
        public string AccessToken { get; set; }
        public string DeviceId { get; set; }
        public RefreshLoginCommand(string refreshToken, string accessToken, string deviceId)
        {
            RefreshToken = refreshToken;
            AccessToken = accessToken;
            DeviceId = deviceId;
        }
    }
}