using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Message;

public sealed class NotificationSentMessage : AizenBaseMessage
{
    public long                NotificationId  { get; set; }
    public long                RecipientUserId { get; set; }
    public NotificationType    Type            { get; set; }
    public NotificationChannel Channel         { get; set; }
    public NotificationStatus  Status          { get; set; }
    public string              Title           { get; set; } = default!;
    public DateTimeOffset      SentAt          { get; set; }
}
