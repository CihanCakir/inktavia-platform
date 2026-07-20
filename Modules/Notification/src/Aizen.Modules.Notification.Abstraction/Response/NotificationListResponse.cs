using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Abstraction.Response;

public sealed class NotificationListResponse
{
    public List<NotificationDto> Items       { get; init; } = [];
    public int                   Total       { get; init; }
    public int                   UnreadCount { get; init; }
}
