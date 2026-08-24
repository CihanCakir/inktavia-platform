using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

[DocumentationInfo("Get notification template variables query handler",
    "Template tipine göre izin verilen {{placeholder}} kataloğunu proxy eder.")]
public sealed class GetTemplateVariablesBffQueryHandler
    : AizenQueryHandler<GetTemplateVariablesBffQuery, NotificationTemplateVariablesDto>
{
    private readonly INotificationTemplateRemoteCall _notification;

    public GetTemplateVariablesBffQueryHandler(INotificationTemplateRemoteCall notification)
        => _notification = notification;

    public override async Task<NotificationTemplateVariablesDto?> Handle(
        GetTemplateVariablesBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.GetTemplateVariables(request.Code);
        return result?.Body;
    }
}
