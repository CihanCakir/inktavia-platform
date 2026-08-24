using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>Admin sayfalı listede tek bir mantıksal template özeti.</summary>
public sealed class NotificationTemplateListItemDto
{
    public long                Id           { get; init; }
    public string              TemplateCode { get; init; } = default!;
    public string              Name         { get; init; } = default!;
    public string?             Description  { get; init; }
    public NotificationType    Type         { get; init; }
    public NotificationChannel Channel      { get; init; }
    public bool                IsActive     { get; init; }
}
