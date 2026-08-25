using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

[DocumentationInfo("Get admin notification history detail query handler",
    "Notification modülünden tek bildirimin tam satırını getirir (gövde + metadata dahil). Yoksa null.")]
public sealed class GetNotificationHistoryDetailBffQueryHandler
    : AizenQueryHandler<GetNotificationHistoryDetailBffQuery, NotificationHistoryDetailDto>
{
    private readonly INotificationRemoteCall _notification;

    public GetNotificationHistoryDetailBffQueryHandler(INotificationRemoteCall notification)
        => _notification = notification;

    public override async Task<NotificationHistoryDetailDto?> Handle(
        GetNotificationHistoryDetailBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.GetNotificationHistoryByIdAsync(request.Id, cancellationToken);
        return result?.Body;
    }
}
