using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Query;

[DocumentationInfo("Get admin notification templates query handler", "Fetches all notification templates from the Notification module.")]
public sealed class GetAdminNotificationTemplatesQueryHandler
    : AizenQueryHandler<GetAdminNotificationTemplatesQuery, List<NotificationTemplateDto>>
{
    private readonly INotificationAdminBffRemoteCall _notification;

    public GetAdminNotificationTemplatesQueryHandler(
        INotificationAdminBffRemoteCall notification)
    {
        _notification        = notification;
    }

    public override async Task<List<NotificationTemplateDto>?> Handle(
        GetAdminNotificationTemplatesQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.GetNotificationTemplates();
        return result?.Body;
    }
}
