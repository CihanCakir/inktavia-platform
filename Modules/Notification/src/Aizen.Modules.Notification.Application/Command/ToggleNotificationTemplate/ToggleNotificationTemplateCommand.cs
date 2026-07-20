using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Command.ToggleNotificationTemplate;

public sealed class ToggleNotificationTemplateCommand : AizenCommand<NotificationTemplateMutationResponse>
{
    public string Code { get; set; } = default!;
}
