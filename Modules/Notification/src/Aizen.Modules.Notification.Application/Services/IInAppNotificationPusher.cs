namespace Aizen.Modules.Notification.Application.Services;

public interface IInAppNotificationPusher
{
    Task PushToUserAsync(long userId, InAppNotificationPayload payload, CancellationToken ct);
}

public sealed class InAppNotificationPayload
{
    public long           NotificationId { get; init; }
    public string         Type           { get; init; } = default!;
    public string         Title          { get; init; } = default!;
    public string         Body           { get; init; } = default!;
    public string?        MetadataJson   { get; init; }
    public DateTimeOffset CreatedAt      { get; init; }
}
