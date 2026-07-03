namespace Aizen.Modules.CargoDry.Abstraction.Enum;

/// <summary>
/// Lifecycle status of a CargoDry consignment sell-through settlement batch.
/// A settlement groups multiple <see cref="CargoDrySalesAttributionStatus.SettlementPending"/> attribution
/// records for a single consignment agreement period into one payout-ready record.
/// </summary>
public enum CargoDrySellThroughSettlementStatus
{
    /// <summary>Settlement record created; attribution records being aggregated.</summary>
    Pending = 1,

    /// <summary>All attributions collected; amounts calculated; ready for finance approval.</summary>
    ReadyForSettlement = 2,

    /// <summary>Finance has scheduled the provider payout for a specific date.</summary>
    Scheduled = 3,

    /// <summary>Provider payout has been executed successfully.</summary>
    Settled = 4,

    /// <summary>Settlement cancelled before payout (e.g. agreement terminated early).</summary>
    Cancelled = 5,

    /// <summary>Provider has raised a dispute on the settlement amounts.</summary>
    Disputed = 6,
}
