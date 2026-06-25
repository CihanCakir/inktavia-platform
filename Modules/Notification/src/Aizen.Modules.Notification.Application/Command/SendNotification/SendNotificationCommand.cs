using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Application.Command.SendNotification;

public sealed class SendNotificationCommand : AizenCommand<SendNotificationCommandResponse>
{
    public long                       RecipientUserId { get; set; }
    public NotificationType           Type            { get; set; }
    public NotificationChannel        Channel         { get; set; }
    public Dictionary<string, string> Variables       { get; set; } = new();
    public string?                    MetadataJson    { get; set; }
}

public sealed class SendNotificationCommandResponse
{
    public long NotificationId { get; init; }
    public bool Dispatched     { get; init; }
}
