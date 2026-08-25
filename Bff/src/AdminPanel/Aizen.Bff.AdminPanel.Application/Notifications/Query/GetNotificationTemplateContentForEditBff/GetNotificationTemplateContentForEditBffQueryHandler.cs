using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

[DocumentationInfo("Get admin notification template content-for-edit query handler",
    "Bir hücrenin (channel×locale) düzenlenebilir mevcut içeriğini (Draft öncelikli, yoksa Published) Notification modülünden getirir.")]
public sealed class GetNotificationTemplateContentForEditBffQueryHandler
    : AizenQueryHandler<GetNotificationTemplateContentForEditBffQuery, NotificationTemplateContentEditResult>
{
    private readonly INotificationTemplateRemoteCall _notification;

    public GetNotificationTemplateContentForEditBffQueryHandler(INotificationTemplateRemoteCall notification)
        => _notification = notification;

    public override async Task<NotificationTemplateContentEditResult?> Handle(
        GetNotificationTemplateContentForEditBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.GetTemplateContentForEdit(request.Code, request.Channel, request.Locale);
        return result?.Body;
    }
}
