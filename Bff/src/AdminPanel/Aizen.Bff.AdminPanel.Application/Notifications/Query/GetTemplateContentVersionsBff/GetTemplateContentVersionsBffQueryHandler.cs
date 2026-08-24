using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

[DocumentationInfo("Get notification template content versions query handler",
    "Bir (channel, locale) için sürüm listesini proxy eder.")]
public sealed class GetTemplateContentVersionsBffQueryHandler
    : AizenQueryHandler<GetTemplateContentVersionsBffQuery, List<NotificationTemplateVersionDto>>
{
    private readonly INotificationTemplateRemoteCall _notification;

    public GetTemplateContentVersionsBffQueryHandler(INotificationTemplateRemoteCall notification)
        => _notification = notification;

    public override async Task<List<NotificationTemplateVersionDto>?> Handle(
        GetTemplateContentVersionsBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.GetTemplateVersions(request.Code, request.Channel, request.Locale);
        return result?.Body;
    }
}
