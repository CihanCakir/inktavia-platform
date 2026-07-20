namespace Aizen.Modules.Notification.Abstraction.Response;

public sealed class SendNotificationResponse
{
    public long NotificationId { get; init; }
    public bool Dispatched     { get; init; }
}
