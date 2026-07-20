namespace Aizen.Modules.Notification.Abstraction.Response;

public sealed class MarkNotificationReadResponse
{
    public long NotificationId { get; init; }
    public bool Updated        { get; init; }
}
