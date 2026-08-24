using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationTemplateDetail;

public sealed class GetNotificationTemplateDetailQuery : AizenQuery<NotificationTemplateDetailDto>
{
    public string Code { get; init; } = default!;
}
