using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

public sealed class NotificationDto
{
    public long                Id           { get; init; }
    public NotificationType    Type         { get; init; }
    public NotificationChannel Channel      { get; init; }
    public NotificationStatus  Status       { get; init; }
    public string              Title        { get; init; } = default!;
    public string              Body         { get; init; } = default!;
    public string?             MetadataJson { get; init; }
    public string?             ReferenceType { get; init; }
    public long?               ReferenceId   { get; init; }
    public bool                IsRead       { get; init; }
    public DateTimeOffset      CreatedAt    { get; init; }
    public DateTimeOffset?     ReadAt       { get; init; }
}
