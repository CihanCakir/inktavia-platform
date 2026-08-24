using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

public sealed class GetTemplateVariablesBffQuery : AizenQuery<NotificationTemplateVariablesDto>
{
    public string Code { get; }

    public GetTemplateVariablesBffQuery(string code) => Code = code;
}
