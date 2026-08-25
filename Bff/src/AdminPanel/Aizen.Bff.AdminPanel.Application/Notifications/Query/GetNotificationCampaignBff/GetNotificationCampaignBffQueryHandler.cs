using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

[DocumentationInfo("Get notification campaign detail query handler",
    "Notification modülünden tek kampanya detayını (durum + sayaçlar) getirir. Yoksa null.")]
public sealed class GetNotificationCampaignBffQueryHandler
    : AizenQueryHandler<GetNotificationCampaignBffQuery, NotificationCampaignDto>
{
    private readonly INotificationRemoteCall _notification;

    public GetNotificationCampaignBffQueryHandler(INotificationRemoteCall notification)
        => _notification = notification;

    public override async Task<NotificationCampaignDto?> Handle(
        GetNotificationCampaignBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.GetNotificationCampaignByIdAsync(request.Id);
        return result?.Body;
    }
}
