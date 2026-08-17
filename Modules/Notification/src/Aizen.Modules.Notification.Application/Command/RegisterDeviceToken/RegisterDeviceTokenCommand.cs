using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Command.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommand : AizenCommand<DeviceTokenResponse>
{
    public string       DeviceToken { get; set; } = default!;
    public PushPlatform Platform    { get; set; }
}
