using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Request;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Command;

[DocumentationInfo("Save notification template content draft command handler",
    "Bir (channel, locale) için taslak içeriği kaydeder (mevcut Draft güncellenir ya da yeni Draft sürüm).")]
public sealed class SaveTemplateContentDraftBffCommandHandler
    : AizenCommandHandler<SaveTemplateContentDraftBffCommand, NotificationTemplateContentDto>
{
    private readonly INotificationTemplateRemoteCall _notification;

    public SaveTemplateContentDraftBffCommandHandler(INotificationTemplateRemoteCall notification)
        => _notification = notification;

    public override async Task<NotificationTemplateContentDto?> Handle(
        SaveTemplateContentDraftBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _notification.SaveTemplateContentDraft(
            request.Code, request.Channel, request.Locale,
            new SaveNotificationTemplateContentRequest
            {
                SubjectTemplate  = request.SubjectTemplate,
                HtmlTemplate     = request.HtmlTemplate,
                TextTemplate     = request.TextTemplate,
                LayoutCode       = request.LayoutCode,
                TitleTemplate    = request.TitleTemplate,
                BodyTemplate     = request.BodyTemplate,
                DeepLinkTemplate = request.DeepLinkTemplate,
                SmsTextTemplate  = request.SmsTextTemplate,
            });

        return result?.Body;
    }
}
