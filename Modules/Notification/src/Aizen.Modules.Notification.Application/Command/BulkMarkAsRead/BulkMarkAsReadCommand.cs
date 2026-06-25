using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Notification.Application.Command.BulkMarkAsRead;

public sealed class BulkMarkAsReadCommand : AizenCommand<bool>
{
    public long UserId { get; set; }
}
