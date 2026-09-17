namespace Aizen.Modules.CargoDry.Abstraction.Interface.Service;

/// <summary>
/// Second-pass healer for the sell-through settlement link. Finds ConsignmentSellThrough sales attributions in a period
/// whose SellThroughSettlementId is still null (orphans — e.g. created before link-at-activation, or where a settlement
/// insert failed), then creates the period settlement if missing and links every orphan to it. Idempotent: once linked,
/// a re-run finds nothing. Invoked by the monthly settlement automation before it processes settlements, so newly
/// created/healed settlements are picked up in the same run.
/// </summary>
public interface ICargoDrySettlementLinkingService
{
    /// <summary>
    /// Links all unlinked ConsignmentSellThrough attributions for the given target year-month (YYYYMM).
    /// Returns the number of attributions newly linked.
    /// </summary>
    Task<int> LinkUnlinkedForPeriodAsync(int targetYearMonth, long triggeredByUserId, CancellationToken ct);
}
