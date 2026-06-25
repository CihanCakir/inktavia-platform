using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Query;

[DocumentationInfo("Get admin notification template by code query handler", "Fetches a single notification template by its code from the Notification module.")]
public sealed class GetAdminNotificationTemplateByCodeQueryHandler
    : AizenQueryHandler<GetAdminNotificationTemplateByCodeQuery, NotificationTemplateDto>
{
    private readonly INotificationAdminBffRemoteCall _notification;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminNotificationTemplateByCodeQueryHandler(
        INotificationAdminBffRemoteCall notification,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _notification        = notification;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<NotificationTemplateDto?> Handle(
        GetAdminNotificationTemplateByCodeQuery request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var result = await _notification.GetNotificationTemplateByCode(
            request.Code, $"Bearer {serviceToken}", request.UserToken);
        return result?.Body;
    }
}
