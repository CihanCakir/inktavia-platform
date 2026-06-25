using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Command;

[DocumentationInfo("Create notification template command handler", "Creates a new notification template via the Notification module.")]
public sealed class CreateNotificationTemplateCommandHandler
    : AizenCommandHandler<CreateNotificationTemplateCommand, AdminBffCommandResultDto>
{
    private readonly INotificationAdminBffRemoteCall _notification;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public CreateNotificationTemplateCommandHandler(
        INotificationAdminBffRemoteCall notification,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _notification        = notification;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        CreateNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        await _notification.CreateNotificationTemplate(
            new CreateNotificationTemplateRemoteRequest
            {
                TemplateCode  = request.TemplateCode,
                Name          = request.Name,
                Type          = request.Type,
                Channel       = request.Channel,
                TitleTemplate = request.TitleTemplate,
                BodyTemplate  = request.BodyTemplate,
            },
            $"Bearer {serviceToken}",
            request.UserToken);

        return AdminBffCommandResultDto.Ok();
    }
}
