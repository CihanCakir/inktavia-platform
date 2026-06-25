using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Application.Command.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommand : AizenCommand<bool>
{
    public long         UserId      { get; set; }
    public string       DeviceToken { get; set; } = default!;
    public PushPlatform Platform    { get; set; }
}
