using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Query.GetUserNotifications;

public sealed class GetUserNotificationsQuery : AizenQuery<NotificationListResponse>
{
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
}
