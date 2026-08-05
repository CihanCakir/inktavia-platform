namespace Aizen.Modules.Notification.Abstraction.Enum;

/// <summary>
/// User-facing grouping of <see cref="NotificationType"/> for preference toggles (N-B).
/// A user mutes/unmutes a whole category per channel, not individual types.
/// </summary>
public enum NotificationCategory
{
    Messages        = 1,
    ServiceRequests = 2,
    Disputes        = 3,
    Payments        = 4,
    CargoDry        = 5,
    /// <summary>Profile + auth/OTP (security). Always-deliver — excluded from the user toggle list.</summary>
    Account         = 6,
    Broadcast       = 7,
}
