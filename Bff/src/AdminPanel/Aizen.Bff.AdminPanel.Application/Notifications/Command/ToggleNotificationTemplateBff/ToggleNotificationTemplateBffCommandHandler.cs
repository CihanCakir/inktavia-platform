using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Command;

[DocumentationInfo("Toggle notification template command handler", "Toggles the active state of a notification template via the Notification module.")]
public sealed class ToggleNotificationTemplateBffCommandHandler
    : AizenCommandHandler<ToggleNotificationTemplateBffCommand, AdminBffCommandResultDto>
{
    private readonly INotificationTemplateRemoteCall _notification;

    public ToggleNotificationTemplateBffCommandHandler(
        INotificationTemplateRemoteCall notification)
    {
        _notification        = notification;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        ToggleNotificationTemplateBffCommand request, CancellationToken cancellationToken)
    {
        await _notification.ToggleNotificationTemplate(
            request.Code);
        return AdminBffCommandResultDto.Ok();
    }
}
