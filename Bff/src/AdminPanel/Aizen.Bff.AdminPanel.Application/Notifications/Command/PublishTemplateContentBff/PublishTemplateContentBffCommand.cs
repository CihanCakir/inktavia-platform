using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Command;

public sealed class PublishTemplateContentBffCommand : AizenCommand<NotificationTemplateContentDto>
{
    public string              Code    { get; init; } = default!;
    public NotificationChannel Channel { get; init; }
    public string              Locale  { get; init; } = default!;
}
