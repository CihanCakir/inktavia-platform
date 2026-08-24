using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Command.UpdateNotificationTemplateMeta;

public sealed class UpdateNotificationTemplateMetaCommand : AizenCommand<NotificationTemplateMutationResponse>
{
    public string  Code        { get; set; } = default!;
    public string  Name        { get; set; } = default!;
    public string? Description  { get; set; }
    public bool    IsActive    { get; set; }
}
