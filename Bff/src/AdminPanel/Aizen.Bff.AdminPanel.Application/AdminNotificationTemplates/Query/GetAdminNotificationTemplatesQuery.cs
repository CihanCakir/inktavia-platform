using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Query;

public sealed class GetAdminNotificationTemplatesQuery : AizenQuery<List<NotificationTemplateDto>>
{

    public GetAdminNotificationTemplatesQuery() { }
}
