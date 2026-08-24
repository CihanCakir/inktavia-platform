using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Application.Query.GetTemplateVariables;

public sealed class GetTemplateVariablesQuery : AizenQuery<NotificationTemplateVariablesDto>
{
    public string Code { get; init; } = default!;
}
