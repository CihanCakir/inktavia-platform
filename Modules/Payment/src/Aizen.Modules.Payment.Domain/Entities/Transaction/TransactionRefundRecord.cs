using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Transaction;

/// <summary>
/// Represents a single refund operation (full or partial) on a PaymentTransaction.
///
/// Design rules:
///   - One transaction can have multiple RefundRecords (for multiple partial refunds).
///   - TotalRefundedAmount on the parent is always the sum of all Processed (non-Reversed) records.
///   - A RefundRecord can be Reversed only while in Processed state and before bank settlement.
///   - Reversal does not delete the record — it marks it as Reversed for full audit trail.
///
/// State machine:
///   Pending → Processed (gateway confirms refund)
///   Pending → Failed    (gateway rejects refund)
///   Processed → Reversed (admin reversal before bank settlement)
/// </summary>
[DocumentationInfo("Transaction refund record",
    "Tracks each individual refund operation (full or partial) with full audit and reversal support.")]
[NoMessagebusSync] // domain-authored immutable refund record — never generically writable
public sealed class TransactionRefundRecord : AizenEntityWithAudit
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public long   PaymentTransactionId    { get; private set; }
    public string RefundCode              { get; private set; } = default!; // REF-YYYYMMDD-XXXX

    // ── Classification ────────────────────────────────────────────────────────
    public RefundType             RefundType   { get; private set; }
    public RefundReason           Reason       { get; private set; }
    public TransactionRefundStatus Status      { get; private set; }

    // ── BE-P10: allocation + cause/release-state + benefit-restore-once guard ──
    public RefundCause?   Cause                 { get; private set; }
    public ReleaseState?  ReleaseState          { get; private set; }
    public long?          RefundAllocationId    { get; private set; }
    public bool           BenefitRestoreApplied { get; private set; }

    // ── Amount ────────────────────────────────────────────────────────────────
    public decimal Amount                 { get; private set; }
    public string  CurrencyCode           { get; private set; } = "TRY";

    // ── Gateway ───────────────────────────────────────────────────────────────
    public string? GatewayRefundReference { get; private set; }

    // ── Processing ────────────────────────────────────────────────────────────
    public DateTime? ProcessedAt          { get; private set; }
    public string?   FailureReason        { get; private set; }
    public string?   AdminNote            { get; private set; }

    // ── Reversal ──────────────────────────────────────────────────────────────
    public DateTime? ReversedAt           { get; private set; }
    public string?   ReversalReason       { get; private set; }
    public string?   ReversalAdminNote    { get; private set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public PaymentTransactionEntity Transaction { get; private set; } = default!;

    private TransactionRefundRecord() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static TransactionRefundRecord Create(
        long        paymentTransactionId,
        string      refundCode,
        decimal     amount,
        string      currencyCode,
        RefundType  refundType,
        RefundReason reason,
        string?     adminNote = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Refund amount must be positive.", nameof(amount));

        return new TransactionRefundRecord
        {
            PaymentTransactionId = paymentTransactionId,
            RefundCode           = refundCode,
            Amount               = amount,
            CurrencyCode         = currencyCode.ToUpperInvariant(),
            RefundType           = refundType,
            Reason               = reason,
            Status               = TransactionRefundStatus.Pending,
            AdminNote            = adminNote,
            IsActive             = true,
        };
    }

    // ── Domain transitions ────────────────────────────────────────────────────

    /// <summary>
    /// Marks the refund as successfully processed by the gateway.
    /// </summary>
    public void MarkProcessed(string? gatewayRefundReference, string? adminNote = null)
    {
        if (Status != TransactionRefundStatus.Pending)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundRecordInvalidState);

        Status                = TransactionRefundStatus.Processed;
        GatewayRefundReference = gatewayRefundReference;
        ProcessedAt           = DateTime.UtcNow;
        if (adminNote is not null) AdminNote = adminNote;
    }

    /// <summary>
    /// Marks the refund as failed (gateway rejected).
    /// </summary>
    public void MarkFailed(string reason)
    {
        if (Status != TransactionRefundStatus.Pending)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundRecordInvalidState);

        Status        = TransactionRefundStatus.Failed;
        FailureReason = reason;
        ProcessedAt   = DateTime.UtcNow;
    }

    /// <summary>
    /// Reverses a processed refund (cancels the refund — money goes back to platform).
    /// Only allowed while Status = Processed and before bank settlement.
    ///
    /// After reversal, the parent transaction's TotalRefundedAmount must be recalculated.
    /// </summary>
    public void Reverse(string reversalReason, string? adminNote = null)
    {
        if (Status != TransactionRefundStatus.Processed)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundReversalInvalidState);

        Status             = TransactionRefundStatus.Reversed;
        ReversedAt         = DateTime.UtcNow;
        ReversalReason     = reversalReason;
        ReversalAdminNote  = adminNote;
    }

    /// <summary>BE-P10 §7.5 — links the persisted 9-amount allocation + records the economic cause + release state.</summary>
    public void SetAllocation(RefundCause cause, ReleaseState releaseState, long refundAllocationId)
    {
        Cause              = cause;
        ReleaseState       = releaseState;
        RefundAllocationId = refundAllocationId;
    }

    /// <summary>BE-P10 §19.15 — marks the benefit/entitlement restore as applied EXACTLY ONCE (idempotent per refund).</summary>
    public void MarkBenefitRestoreApplied()
    {
        if (BenefitRestoreApplied)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundRestoreAlreadyApplied);
        BenefitRestoreApplied = true;
    }
}
