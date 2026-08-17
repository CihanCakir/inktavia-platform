using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Command.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadCommand : AizenCommand<MarkNotificationReadResponse>
{
    public long NotificationId { get; set; }
}
