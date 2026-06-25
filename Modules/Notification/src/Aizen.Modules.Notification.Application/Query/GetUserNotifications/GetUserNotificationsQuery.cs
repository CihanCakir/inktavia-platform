using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Application.Query.GetUserNotifications;

public sealed class GetUserNotificationsQuery : AizenQuery<GetUserNotificationsResponse>
{
    public long UserId { get; set; }
    public int  Skip   { get; set; } = 0;
    public int  Take   { get; set; } = 20;
}

public sealed class GetUserNotificationsResponse
{
    public List<NotificationDto> Items       { get; init; } = [];
    public int                   Total       { get; init; }
    public int                   UnreadCount { get; init; }
}
