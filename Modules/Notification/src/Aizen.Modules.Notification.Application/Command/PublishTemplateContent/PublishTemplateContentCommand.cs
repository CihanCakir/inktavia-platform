using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Application.Command.PublishTemplateContent;

public sealed class PublishTemplateContentCommand : AizenCommand<NotificationTemplateContentDto>
{
    public string              Code    { get; set; } = default!;
    public NotificationChannel Channel { get; set; }
    public string              Locale  { get; set; } = default!;
}
