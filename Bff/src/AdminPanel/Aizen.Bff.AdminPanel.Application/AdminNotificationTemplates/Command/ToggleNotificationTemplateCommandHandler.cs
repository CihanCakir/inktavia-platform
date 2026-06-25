using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Command;

[DocumentationInfo("Toggle notification template command handler", "Toggles the active state of a notification template via the Notification module.")]
public sealed class ToggleNotificationTemplateCommandHandler
    : AizenCommandHandler<ToggleNotificationTemplateCommand, AdminBffCommandResultDto>
{
    private readonly INotificationAdminBffRemoteCall _notification;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public ToggleNotificationTemplateCommandHandler(
        INotificationAdminBffRemoteCall notification,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _notification        = notification;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        ToggleNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        await _notification.ToggleNotificationTemplate(
            request.Code, $"Bearer {serviceToken}", request.UserToken);
        return AdminBffCommandResultDto.Ok();
    }
}
