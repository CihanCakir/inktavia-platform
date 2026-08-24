using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

[DocumentationInfo("Get admin notification template detail query handler",
    "Notification modülünden mantıksal template + içerik matrisini getirir.")]
public sealed class GetNotificationTemplateDetailBffQueryHandler
    : AizenQueryHandler<GetNotificationTemplateDetailBffQuery, NotificationTemplateDetailDto>
{
    private readonly INotificationTemplateRemoteCall _notification;

    public GetNotificationTemplateDetailBffQueryHandler(INotificationTemplateRemoteCall notification)
        => _notification = notification;

    public override async Task<NotificationTemplateDetailDto?> Handle(
        GetNotificationTemplateDetailBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.GetTemplateDetail(request.Code);
        return result?.Body;
    }
}
