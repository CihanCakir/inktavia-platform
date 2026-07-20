using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationTemplateByCode;

public sealed class GetNotificationTemplateByCodeQuery : AizenQuery<NotificationTemplateDto?>
{
    public string Code { get; set; } = default!;
}
