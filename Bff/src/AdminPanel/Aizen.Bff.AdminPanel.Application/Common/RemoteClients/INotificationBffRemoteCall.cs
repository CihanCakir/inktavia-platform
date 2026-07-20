using Aizen.Bff.AdminPanel.Application.Notifications.Dto;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("Notification user BFF remote call", "Forwards authenticated user's notification inbox requests to the Notification module. Uses AuthorizationForwardingHandler to propagate the user's Bearer token.")]
public interface INotificationBffRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/notification/notifications")]
    Task<AizenApiResponse<NotificationListBffDto>> GetMyNotificationsAsync(
        [Refit.Query] int skip     = 0,
        [Refit.Query] int take     = 20,
        CancellationToken ct = default);

    [AizenRemoteCallPatch("/api/v1/notification/notifications/{id}/read")]
    Task<AizenApiResponse<object>> MarkReadAsync(long id, CancellationToken ct = default);

    [AizenRemoteCallPost("/api/v1/notification/notifications/mark-all-read")]
    Task<AizenApiResponse<object>> MarkAllReadAsync(CancellationToken ct = default);
}
