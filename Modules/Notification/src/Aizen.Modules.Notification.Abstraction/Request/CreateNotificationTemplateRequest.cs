using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Request;

public sealed class CreateNotificationTemplateRequest
{
    public string              TemplateCode  { get; set; } = default!;
    public string              Name          { get; set; } = default!;
    public NotificationType    Type          { get; set; }
    public NotificationChannel Channel       { get; set; }
    public string              TitleTemplate { get; set; } = default!;
    public string              BodyTemplate  { get; set; } = default!;
}
