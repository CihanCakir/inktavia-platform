using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.NotificationTemplates.Query;

public sealed class GetNotificationTemplatesBffQuery : AizenQuery<List<NotificationTemplateDto>>
{

    public GetNotificationTemplatesBffQuery() { }
}
