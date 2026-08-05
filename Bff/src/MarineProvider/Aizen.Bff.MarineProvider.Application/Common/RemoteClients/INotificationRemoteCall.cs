using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

public interface INotificationRemoteCall : IAizenRemoteCall
{
    // Body MUST be the module's nested PushSubscriptionRequest ({ endpoint, keys: { p256dh, auth } }).
    // A flat { endpoint, p256dh, auth } body deserialized into the module DTO with a null Keys → the module's
    // `body.Keys.P256dh` NRE'd → 500 on subscribe. This is the shape the module controller binds.
    [AizenRemoteCallPost("/api/v1/notification/notifications/push-subscriptions")]
    Task<AizenApiResponse<PushSubscriptionResponse>> RegisterWebPushSubscription(
        [AizenRemoteCallBody] PushSubscriptionRequest body);

    [AizenRemoteCallDelete("/api/v1/notification/notifications/push-subscriptions")]
    Task<AizenApiResponse<PushSubscriptionResponse>> DeactivateWebPushSubscription(
        [AizenRemoteCallBody] PushUnsubscribeRequest body);

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
