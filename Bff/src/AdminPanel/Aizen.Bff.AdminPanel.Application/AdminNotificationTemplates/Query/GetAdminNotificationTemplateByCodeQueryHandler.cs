using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Query;

[DocumentationInfo("Get admin notification template by code query handler", "Fetches a single notification template by its code from the Notification module.")]
public sealed class GetAdminNotificationTemplateByCodeQueryHandler
    : AizenQueryHandler<GetAdminNotificationTemplateByCodeQuery, NotificationTemplateDto>
{
    private readonly INotificationAdminBffRemoteCall _notification;

    public GetAdminNotificationTemplateByCodeQueryHandler(
        INotificationAdminBffRemoteCall notification)
    {
        _notification        = notification;
    }

    public override async Task<NotificationTemplateDto?> Handle(
        GetAdminNotificationTemplateByCodeQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.GetNotificationTemplateByCode(
            request.Code);
        return result?.Body;
    }
}
