using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

public sealed class NotificationTemplateDto
{
    public long                Id            { get; init; }
    public string              TemplateCode  { get; init; } = default!;
    public string              Name          { get; init; } = default!;
    public NotificationType    Type          { get; init; }
    public NotificationChannel Channel       { get; init; }
    public string              TitleTemplate { get; init; } = default!;
    public string              BodyTemplate  { get; init; } = default!;
    public bool                IsActive      { get; init; }
}
