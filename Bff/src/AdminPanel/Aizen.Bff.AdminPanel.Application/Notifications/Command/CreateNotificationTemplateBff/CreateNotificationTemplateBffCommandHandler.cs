using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Command;

[DocumentationInfo("Create notification template command handler", "Creates a new notification template via the Notification module.")]
public sealed class CreateNotificationTemplateBffCommandHandler
    : AizenCommandHandler<CreateNotificationTemplateBffCommand, AdminBffCommandResultDto>
{
    private readonly INotificationTemplateRemoteCall _notification;

    public CreateNotificationTemplateBffCommandHandler(
        INotificationTemplateRemoteCall notification)
    {
        _notification        = notification;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        CreateNotificationTemplateBffCommand request, CancellationToken cancellationToken)
    {
        await _notification.CreateNotificationTemplate(
            new CreateNotificationTemplateRemoteRequest
            {
                TemplateCode  = request.TemplateCode,
                Name          = request.Name,
                Type          = request.Type,
                Channel       = request.Channel,
                TitleTemplate = request.TitleTemplate,
                BodyTemplate  = request.BodyTemplate,
            });

        return AdminBffCommandResultDto.Ok();
    }
}
