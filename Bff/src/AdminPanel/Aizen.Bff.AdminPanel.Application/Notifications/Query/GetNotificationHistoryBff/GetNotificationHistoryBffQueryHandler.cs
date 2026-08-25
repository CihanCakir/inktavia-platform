using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

[DocumentationInfo("Get admin notification history paged query handler",
    "Notification modülünün sayfalı admin gönderim geçmişini proxy eder (tüm kanallar).")]
public sealed class GetNotificationHistoryBffQueryHandler
    : AizenQueryHandler<GetNotificationHistoryBffQuery, NotificationHistoryListResult>
{
    private readonly INotificationRemoteCall _notification;

    public GetNotificationHistoryBffQueryHandler(INotificationRemoteCall notification)
        => _notification = notification;

    public override async Task<NotificationHistoryListResult> Handle(
        GetNotificationHistoryBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.GetNotificationHistoryAsync(
            request.From, request.To, request.Channel, request.Status,
            request.TemplateCode, request.RecipientUserId, request.CampaignId, request.Page, request.PageSize, cancellationToken);

        // Gövde null gelirse sözleşmeyi koruyacak boş sayfa (paged template handler ile aynı yaklaşım).
        return result?.Body ?? new NotificationHistoryListResult
        {
            Items      = new List<NotificationHistoryListItemDto>(),
            TotalCount = 0,
            Page       = request.Page,
            PageSize   = request.PageSize,
        };
    }
}
