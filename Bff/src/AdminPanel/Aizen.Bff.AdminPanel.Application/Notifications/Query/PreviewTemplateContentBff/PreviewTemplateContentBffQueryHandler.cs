using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Request;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

[DocumentationInfo("Preview notification template content query handler",
    "ÜRETİMLE aynı renderer ile önizleme; sonuç (render/eksik anahtarlar) gövdede. 400'ü controller üretir.")]
public sealed class PreviewTemplateContentBffQueryHandler
    : AizenQueryHandler<PreviewTemplateContentBffQuery, NotificationTemplatePreviewResultDto>
{
    private readonly INotificationTemplateRemoteCall _notification;

    public PreviewTemplateContentBffQueryHandler(INotificationTemplateRemoteCall notification)
        => _notification = notification;

    public override async Task<NotificationTemplatePreviewResultDto?> Handle(
        PreviewTemplateContentBffQuery request, CancellationToken cancellationToken)
    {
        var result = await _notification.PreviewTemplateContent(
            request.Code,
            new NotificationTemplatePreviewRequest
            {
                Channel   = request.Channel,
                Locale    = request.Locale,
                Variables = request.Variables,
            });

        return result?.Body;
    }
}
