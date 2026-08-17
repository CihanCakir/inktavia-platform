using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Transaction;

namespace Aizen.Modules.Payment.Domain.Entities.RefundAllocation;

/// <summary>
/// BE-S13b — pure guards for driving the P10 path from a dispute resolution. The dispute context ref
/// <c>DISPUTE-{disputeId}</c> is stamped on the refund record's AdminNote; a re-resolve that finds a non-Failed
/// record already carrying that ref is idempotent (no second refund). Refundable-amount validation is exact and
/// side-effect-free so it is unit-testable without a DB.
/// </summary>
public static class DisputeRefundGuard
{
    /// <summary>The idempotency ref stamped on the dispute-driven refund record's AdminNote.</summary>
    public static string ContextRef(long disputeId) => $"DISPUTE-{disputeId}";

    /// <summary>A partial/split dispute refund amount is valid when strictly positive and ≤ refundable.</summary>
    public static bool IsRefundableAmountValid(decimal requestedAmount, decimal refundableAmount)
        => requestedAmount > 0m && requestedAmount <= refundableAmount;

    /// <summary>
    /// True when a prior resolve already applied this dispute's refund — any non-Failed refund record whose AdminNote
    /// starts with the dispute context ref. Guarantees a re-resolve never issues a second refund.
    /// </summary>
    public static bool AlreadyApplied(IEnumerable<TransactionRefundRecord> refundRecords, string contextRef)
        => refundRecords.Any(r =>
            r.Status != TransactionRefundStatus.Failed &&
            r.AdminNote is not null &&
            r.AdminNote.StartsWith(contextRef, StringComparison.Ordinal));
}
