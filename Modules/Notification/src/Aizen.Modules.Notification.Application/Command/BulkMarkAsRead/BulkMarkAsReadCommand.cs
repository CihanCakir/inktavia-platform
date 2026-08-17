using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Command.BulkMarkAsRead;

public sealed class BulkMarkAsReadCommand : AizenCommand<MarkAllNotificationsReadResponse>
{
}
