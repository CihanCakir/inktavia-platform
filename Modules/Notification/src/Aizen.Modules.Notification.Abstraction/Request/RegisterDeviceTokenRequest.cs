using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Request;

public sealed class RegisterDeviceTokenRequest
{
    public string       DeviceToken { get; set; } = default!;
    public PushPlatform Platform    { get; set; }
}
