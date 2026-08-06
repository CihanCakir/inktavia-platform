using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.NotificationTemplates.Query;

public sealed class GetNotificationTemplateByCodeBffQuery : AizenQuery<NotificationTemplateDto>
{
    public string Code      { get; }

    public GetNotificationTemplateByCodeBffQuery(string code)
    {
        Code      = code;
    }
}
