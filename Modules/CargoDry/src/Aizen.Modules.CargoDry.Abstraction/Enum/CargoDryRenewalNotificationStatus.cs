namespace Aizen.Modules.CargoDry.Abstraction.Enum;

/// <summary>
/// Tracks the notification dispatch state within a CargoDry renewal preparation.
/// CargoDry owns only the reference and status; actual channel delivery is owned by the Notification module.
/// </summary>
public enum CargoDryRenewalNotificationStatus
{
    /// <summary>No notification action taken yet.</summary>
    None = 0,

    /// <summary>Notification preview/intent prepared; recipient and template resolved but not yet sent.</summary>
    Prepared = 1,

    /// <summary>Message published to the bus; Notification module consumer is expected to process it.</summary>
    Queued = 2,

    /// <summary>Consumer confirmed the notification was dispatched to the delivery channel.</summary>
    Dispatched = 3,

    /// <summary>Partial failure — at least one channel delivered, at least one failed.</summary>
    PartiallyFailed = 4,

    /// <summary>All channels failed to deliver.</summary>
    Failed = 5,
}
