using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Command;

[DocumentationInfo("Toggle notification template command handler", "Toggles the active state of a notification template via the Notification module.")]
public sealed class ToggleNotificationTemplateCommandHandler
    : AizenCommandHandler<ToggleNotificationTemplateCommand, AdminBffCommandResultDto>
{
    private readonly INotificationTemplateRemoteCall _notification;

    public ToggleNotificationTemplateCommandHandler(
        INotificationTemplateRemoteCall notification)
    {
        _notification        = notification;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        ToggleNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        await _notification.ToggleNotificationTemplate(
            request.Code);
        return AdminBffCommandResultDto.Ok();
    }
}
