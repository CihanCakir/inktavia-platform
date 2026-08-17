using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Domain.Interface.Service;

/// <summary>
/// Platform-specific push sender. One implementation per <see cref="Abstraction.Enum.PushPlatform"/>.
/// </summary>
public interface IPushSender
{
    Task<string> SendAsync(UserDeviceTokenEntity subscription, string title, string body, string? dataJson, CancellationToken ct);
}
