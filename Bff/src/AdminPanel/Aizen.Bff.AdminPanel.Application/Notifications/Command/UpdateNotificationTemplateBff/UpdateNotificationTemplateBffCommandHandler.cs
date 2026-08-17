using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Command;

[DocumentationInfo("Update notification template command handler", "Updates an existing notification template via the Notification module.")]
public sealed class UpdateNotificationTemplateBffCommandHandler
    : AizenCommandHandler<UpdateNotificationTemplateBffCommand, AdminBffCommandResultDto>
{
    private readonly INotificationTemplateRemoteCall _notification;

    public UpdateNotificationTemplateBffCommandHandler(
        INotificationTemplateRemoteCall notification)
    {
        _notification        = notification;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        UpdateNotificationTemplateBffCommand request, CancellationToken cancellationToken)
    {
        await _notification.UpdateNotificationTemplate(
            request.Code,
            new UpdateNotificationTemplateRemoteRequest
            {
                Name          = request.Name,
                TitleTemplate = request.TitleTemplate,
                BodyTemplate  = request.BodyTemplate,
            });

        return AdminBffCommandResultDto.Ok();
    }
}
