using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Command;

[DocumentationInfo("Publish notification template content command handler",
    "Bir (channel, locale) taslağını yayınlar (önceki Published arşivlenir).")]
public sealed class PublishTemplateContentBffCommandHandler
    : AizenCommandHandler<PublishTemplateContentBffCommand, NotificationTemplateContentDto>
{
    private readonly INotificationTemplateRemoteCall _notification;

    public PublishTemplateContentBffCommandHandler(INotificationTemplateRemoteCall notification)
        => _notification = notification;

    public override async Task<NotificationTemplateContentDto?> Handle(
        PublishTemplateContentBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _notification.PublishTemplateContent(request.Code, request.Channel, request.Locale);
        return result?.Body;
    }
}
