using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.NotificationTemplates.Query;

[DocumentationInfo("Get admin notification templates query handler", "Fetches all notification templates from the Notification module.")]
public sealed class GetNotificationTemplatesBffQueryHandler
    : AizenQueryHandler<GetNotificationTemplatesBffQuery, List<NotificationTemplateDto>>
{
    private readonly INotificationTemplateRemoteCall _notification;

    public GetNotificationTemplatesBffQueryHandler(
        INotificationTemplateRemoteCall notification)
    {
        _notification        = notification;
    }

    public override async Task<List<NotificationTemplateDto>?> Handle(
        GetNotificationTemplatesBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.GetNotificationTemplates();
        return result?.Body;
    }
}
