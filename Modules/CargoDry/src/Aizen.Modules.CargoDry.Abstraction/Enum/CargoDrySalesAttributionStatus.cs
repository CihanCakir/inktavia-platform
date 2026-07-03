namespace Aizen.Modules.CargoDry.Abstraction.Enum;

/// <summary>
/// Lifecycle status of a CargoDry kit sales attribution record.
/// Created at kit activation; progresses through commercial resolution.
/// </summary>
public enum CargoDrySalesAttributionStatus
{
    /// <summary>Attribution record created; commercial path not yet resolved.</summary>
    Pending = 1,

    /// <summary>Commercial attribution successfully resolved (provider identified, amounts calculated).</summary>
    Attributed = 2,

    /// <summary>Attribution is part of a sell-through settlement awaiting processing.</summary>
    SettlementPending = 3,

    /// <summary>Attribution has been included in a completed settlement.</summary>
    Settled = 4,

    /// <summary>Attribution cancelled (kit revoked, agreement voided, etc.).</summary>
    Cancelled = 5,

    /// <summary>
    /// Commercial path could not be automatically resolved (e.g. SalesChannel is null,
    /// agreement missing). Requires manual admin review.
    /// </summary>
    CommercialReviewRequired = 6,
}
