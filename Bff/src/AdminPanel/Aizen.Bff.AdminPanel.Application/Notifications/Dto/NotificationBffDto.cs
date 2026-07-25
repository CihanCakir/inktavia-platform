namespace Aizen.Bff.AdminPanel.Application.Notifications.Dto;

[DocumentationInfo("Notification BFF DTO", "User-facing in-app notification shape for the frontend inbox. Mirrors NotificationDto from the Notification module.")]
public sealed class NotificationBffDto
{
    public long            Id           { get; init; }
    public string          Type         { get; init; } = default!;
    public string          Title        { get; init; } = default!;
    public string          Body         { get; init; } = default!;
    public bool            IsRead       { get; init; }
    public string?         MetadataJson { get; init; }
    public string?         ReferenceType { get; init; }
    public long?           ReferenceId   { get; init; }
    public DateTimeOffset  CreatedAt    { get; init; }
    public DateTimeOffset? ReadAt       { get; init; }
}

public sealed class NotificationListBffDto
{
    public List<NotificationBffDto> Items       { get; init; } = [];
    public int                      Total       { get; init; }
    public int                      UnreadCount { get; init; }
}
