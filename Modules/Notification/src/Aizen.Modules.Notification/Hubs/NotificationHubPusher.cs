using Aizen.Modules.Notification.Application.Services;
using Microsoft.AspNetCore.SignalR;

namespace Aizen.Modules.Notification.Hubs;

public sealed class NotificationHubPusher : IInAppNotificationPusher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationHubPusher(IHubContext<NotificationHub> hubContext)
        => _hubContext = hubContext;

    public Task PushToUserAsync(long userId, InAppNotificationPayload payload, CancellationToken ct)
        => _hubContext.Clients
            .Group($"user:{userId}")
            .SendAsync("NotificationReceived", payload, ct);
}
