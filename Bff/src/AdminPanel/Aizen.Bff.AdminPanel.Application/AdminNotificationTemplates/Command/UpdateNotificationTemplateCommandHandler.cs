using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Command;

[DocumentationInfo("Update notification template command handler", "Updates an existing notification template via the Notification module.")]
public sealed class UpdateNotificationTemplateCommandHandler
    : AizenCommandHandler<UpdateNotificationTemplateCommand, AdminBffCommandResultDto>
{
    private readonly INotificationAdminBffRemoteCall _notification;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public UpdateNotificationTemplateCommandHandler(
        INotificationAdminBffRemoteCall notification,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _notification        = notification;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        UpdateNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        await _notification.UpdateNotificationTemplate(
            request.Code,
            new UpdateNotificationTemplateRemoteRequest
            {
                Name          = request.Name,
                TitleTemplate = request.TitleTemplate,
                BodyTemplate  = request.BodyTemplate,
            },
            $"Bearer {serviceToken}",
            request.UserToken);

        return AdminBffCommandResultDto.Ok();
    }
}
