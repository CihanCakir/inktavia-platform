using Aizen.Bff.AdminPanel.Application.Notifications.Dto;
using Aizen.Core.RemoteCall.Abstraction;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Notification user BFF remote call", "Forwards authenticated user's notification inbox requests to the Notification module. Uses AuthorizationForwardingHandler to propagate the user's Bearer token.")]
public interface INotificationBffRemoteCall : IAizenRemoteCall
{
    [Get("/api/v1/notification/notifications")]
    Task<NotificationListBffDto> GetMyNotificationsAsync(
        [Query] int skip     = 0,
        [Query] int take     = 20,
        CancellationToken ct = default);

    [Patch("/api/v1/notification/notifications/{id}/read")]
    Task MarkReadAsync(long id, CancellationToken ct = default);

    [Post("/api/v1/notification/notifications/mark-all-read")]
    Task MarkAllReadAsync(CancellationToken ct = default);
}
