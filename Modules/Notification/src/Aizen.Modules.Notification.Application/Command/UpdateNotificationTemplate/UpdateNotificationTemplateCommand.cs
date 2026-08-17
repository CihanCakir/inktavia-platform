using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Command.UpdateNotificationTemplate;

public sealed class UpdateNotificationTemplateCommand : AizenCommand<NotificationTemplateMutationResponse>
{
    public string Code          { get; set; } = default!;
    public string Name          { get; set; } = default!;
    public string TitleTemplate { get; set; } = default!;
    public string BodyTemplate  { get; set; } = default!;
}
