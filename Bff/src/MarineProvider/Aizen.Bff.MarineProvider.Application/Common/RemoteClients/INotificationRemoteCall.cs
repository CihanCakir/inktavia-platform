using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

public interface INotificationRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallPost("/api/v1/notification/notifications/push-subscriptions")]
    Task<AizenApiResponse<PushSubscriptionResponse>> RegisterWebPushSubscription(
        [AizenRemoteCallBody] RegisterWebPushSubscriptionBffRequest body);

    [AizenRemoteCallDelete("/api/v1/notification/notifications/push-subscriptions")]
    Task<AizenApiResponse<PushSubscriptionResponse>> DeactivateWebPushSubscription(
        [AizenRemoteCallBody] DeactivateWebPushSubscriptionBffRequest body);

    [AizenRemoteCallGet("/api/v1/notification/notifications")]
    Task<AizenApiResponse<NotificationListResponse>> GetNotifications(
        [Refit.Query] int skip = 0, [Refit.Query] int take = 20);

    [AizenRemoteCallPost("/api/v1/notification/notifications/mark-all-read")]
    Task<AizenApiResponse<MarkAllNotificationsReadResponse>> MarkAllRead();

    [AizenRemoteCallPatch("/api/v1/notification/notifications/{id}/read")]
    Task<AizenApiResponse<MarkNotificationReadResponse>> MarkRead(long id);

    [AizenRemoteCallGet("/api/v1/notification/notifications/vapid-public-key")]
    Task<AizenApiResponse<VapidPublicKeyResponse>> GetVapidPublicKey();
}

public sealed class RegisterWebPushSubscriptionBffRequest
{
    public string Endpoint { get; set; } = default!;
    public string P256dh { get; set; } = default!;
    public string Auth { get; set; } = default!;
}

public sealed class DeactivateWebPushSubscriptionBffRequest
{
    public string Endpoint { get; set; } = default!;
}
