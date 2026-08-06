using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.NotificationTemplates.Query;

[DocumentationInfo("Get admin notification template by code query handler", "Fetches a single notification template by its code from the Notification module.")]
public sealed class GetNotificationTemplateByCodeBffQueryHandler
    : AizenQueryHandler<GetNotificationTemplateByCodeBffQuery, NotificationTemplateDto>
{
    private readonly INotificationTemplateRemoteCall _notification;

    public GetNotificationTemplateByCodeBffQueryHandler(
        INotificationTemplateRemoteCall notification)
    {
        _notification        = notification;
    }

    public override async Task<NotificationTemplateDto?> Handle(
        GetNotificationTemplateByCodeBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.GetNotificationTemplateByCode(
            request.Code);
        return result?.Body;
    }
}
