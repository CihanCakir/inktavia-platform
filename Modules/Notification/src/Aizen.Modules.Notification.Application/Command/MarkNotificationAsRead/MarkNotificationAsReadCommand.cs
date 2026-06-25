using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Notification.Application.Command.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadCommand : AizenCommand<bool>
{
    public long NotificationId     { get; set; }
    public long RequestingUserId   { get; set; }
}
