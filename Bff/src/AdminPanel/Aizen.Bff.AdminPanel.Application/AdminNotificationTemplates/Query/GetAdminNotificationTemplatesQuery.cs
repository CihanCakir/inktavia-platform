using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Query;

public sealed class GetAdminNotificationTemplatesQuery : AizenQuery<List<NotificationTemplateDto>>
{
    public string UserToken { get; }

    public GetAdminNotificationTemplatesQuery(string userToken)
        => UserToken = userToken;
}
