using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

[DocumentationInfo("Get notification campaigns paged query handler",
    "Notification modülünün sayfalı admin kampanya listesini proxy eder.")]
public sealed class GetNotificationCampaignsPagedBffQueryHandler
    : AizenQueryHandler<GetNotificationCampaignsPagedBffQuery, NotificationCampaignListResult>
{
    private readonly INotificationRemoteCall _notification;

    public GetNotificationCampaignsPagedBffQueryHandler(INotificationRemoteCall notification)
        => _notification = notification;

    public override async Task<NotificationCampaignListResult> Handle(
        GetNotificationCampaignsPagedBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.GetNotificationCampaignsPagedAsync(request.Page, request.PageSize);

        // Gövde null gelirse sözleşmeyi koruyacak boş sayfa.
        return result?.Body ?? new NotificationCampaignListResult
        {
            Items      = new List<NotificationCampaignListItemDto>(),
            TotalCount = 0,
            Page       = request.Page,
            PageSize   = request.PageSize,
        };
    }
}
