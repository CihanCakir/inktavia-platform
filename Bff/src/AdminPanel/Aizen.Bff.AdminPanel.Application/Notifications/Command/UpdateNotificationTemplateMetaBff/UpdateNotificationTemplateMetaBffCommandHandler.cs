using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Request;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Command;

[DocumentationInfo("Update notification template meta command handler",
    "Notification modülünde template meta'sını (ad/açıklama/etkinleştirme) günceller.")]
public sealed class UpdateNotificationTemplateMetaBffCommandHandler
    : AizenCommandHandler<UpdateNotificationTemplateMetaBffCommand, AdminBffCommandResultDto>
{
    private readonly INotificationTemplateRemoteCall _notification;

    public UpdateNotificationTemplateMetaBffCommandHandler(INotificationTemplateRemoteCall notification)
        => _notification = notification;

    public override async Task<AdminBffCommandResultDto?> Handle(
        UpdateNotificationTemplateMetaBffCommand request, CancellationToken cancellationToken)
    {
        await _notification.UpdateTemplateMeta(
            request.Code,
            new UpdateNotificationTemplateMetaRequest
            {
                Name        = request.Name,
                Description  = request.Description,
                IsActive    = request.IsActive,
            });

        return AdminBffCommandResultDto.Ok();
    }
}
