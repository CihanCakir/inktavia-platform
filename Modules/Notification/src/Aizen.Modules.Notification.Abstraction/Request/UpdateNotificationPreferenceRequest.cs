namespace Aizen.Modules.Notification.Abstraction.Request;

/// <summary>
/// N-B — change one preference cell. Category is a <c>NotificationCategory</c> enum name (e.g. "Messages");
/// Channel is a <c>NotificationChannel</c> enum name — only "Push" or "Email" are user-changeable.
/// Locked cells (InApp, or the security Account category) are rejected.
/// </summary>
public sealed class UpdateNotificationPreferenceRequest
{
    public string Category { get; set; } = default!;
    public string Channel  { get; set; } = default!;
    public bool   Enabled  { get; set; }
}
