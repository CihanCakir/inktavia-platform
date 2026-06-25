using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Query;

[DocumentationInfo("Get admin notification templates query handler", "Fetches all notification templates from the Notification module.")]
public sealed class GetAdminNotificationTemplatesQueryHandler
    : AizenQueryHandler<GetAdminNotificationTemplatesQuery, List<NotificationTemplateDto>>
{
    private readonly INotificationAdminBffRemoteCall _notification;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminNotificationTemplatesQueryHandler(
        INotificationAdminBffRemoteCall notification,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _notification        = notification;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<List<NotificationTemplateDto>?> Handle(
        GetAdminNotificationTemplatesQuery request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var result = await _notification.GetNotificationTemplates(
            $"Bearer {serviceToken}", request.UserToken);
        return result?.Body;
    }
}
