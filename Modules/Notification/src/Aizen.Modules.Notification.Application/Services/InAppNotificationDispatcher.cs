using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class InAppNotificationDispatcher : INotificationDispatcher
{
    private readonly IInAppNotificationPusher _pusher;
    private readonly ILogger<InAppNotificationDispatcher> _logger;

    public InAppNotificationDispatcher(
        IInAppNotificationPusher pusher,
        ILogger<InAppNotificationDispatcher> logger)
    {
        _pusher = pusher;
        _logger = logger;
    }

    public async Task DispatchAsync(NotificationEntity notification, CancellationToken ct)
    {
        try
        {
            await _pusher.PushToUserAsync(notification.RecipientUserId, new InAppNotificationPayload
            {
                NotificationId = notification.Id,
                Type           = notification.Type.ToString(),
                Title          = notification.Title,
                Body           = notification.Body,
                MetadataJson   = notification.MetadataJson,
                CreatedAt      = notification.CreatedAt,
            }, ct);

            notification.MarkAsSent();
            _logger.LogInformation(
                "InApp notification dispatched: Id={Id} UserId={UserId}",
                notification.Id, notification.RecipientUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InApp dispatch failed for NotificationId={Id}", notification.Id);
            notification.MarkAsFailed();
        }
    }
}
