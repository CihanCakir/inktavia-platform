using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

public sealed class GetNotificationTemplateDetailBffQuery : AizenQuery<NotificationTemplateDetailDto>
{
    public string Code { get; }

    public GetNotificationTemplateDetailBffQuery(string code) => Code = code;
}
