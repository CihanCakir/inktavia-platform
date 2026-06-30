using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Transaction;

/// <summary>
/// Central record for every money movement in Inktavia Marine OS.
///
/// ── State machine ────────────────────────────────────────────────────────────
///
///  PendingIntent ──[Capture]──► Captured ──[Release]──► Released
///       │                          │                        │
///   [Cancel]                  [ApplyRefund]           [ApplyRefund]
///       │                    (Full/Partial)            (Full/Partial)
///       ▼                          │                        │
///  Cancelled ◄─[Reinstate]         ├──► Refunded            │
///  (→ PendingIntent restored)      └──► PartiallyRefunded ◄─┘
///                                              │
///                                    [ReverseRefund] → back to Captured or Released
///                                      (when TotalRefundedAmount reaches 0)
///
///  PendingIntent ──[MarkFailed]──► Failed
///  Captured or Released ──[Dispute]──► Disputed ──[ResolveDispute]──► Captured/Released
///
/// ── Cancel vs Refund distinction ─────────────────────────────────────────────
///  Cancel()       → PendingIntent only; no money moved; no gateway call.
///  ReinstateCancellation() → Cancelled only; restores to PendingIntent with audit trail.
///  ApplyRefund()  → Captured/Released/PartiallyRefunded; gateway refund must be confirmed first.
///  ReverseRefund()→ Reverses a specific Processed TransactionRefundRecord.
///
/// ── Invariants ───────────────────────────────────────────────────────────────
///  TotalRefundedAmount       = sum of all Processed (not Reversed) RefundRecord.Amount
///  RemainingRefundableAmount = GrossAmount - TotalRefundedAmount
///  Refund amount must never exceed RemainingRefundableAmount.
/// </summary>
[DocumentationInfo("Payment transaction entity",
    "Central record for every money movement. Full state machine: cancel, reinstate, partial refund, and refund reversal.")]
public sealed class PaymentTransactionEntity : AizenEntityWithAudit
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public string          TransactionCode { get; private set; } = default!; // TXN-YYYYMMDD-XXXX
    public TransactionType TransactionType { get; private set; }

    // ── Context ───────────────────────────────────────────────────────────────
    public TransactionContextType ContextType  { get; private set; }
    public long                   ContextId    { get; private set; }
    public long?                  ContextSubId { get; private set; }

    // ── Parties ───────────────────────────────────────────────────────────────
    public long  PayerProfileId     { get; private set; }
    public long? RecipientProfileId { get; private set; }

    // ── Amounts ───────────────────────────────────────────────────────────────
    public decimal GrossAmount            { get; private set; }
    public decimal CommissionAmount       { get; private set; }
    public decimal CommissionRateSnapshot { get; private set; }
    public decimal VatOnCommission        { get; private set; }
    public decimal NetPayoutAmount        { get; private set; }
    public decimal DiscountAmount         { get; private set; }
    public string  CurrencyCode           { get; private set; } = "TRY";

    /// <summary>
    /// Running total of all Processed (non-Reversed) refund amounts.
    /// Maintained by ApplyRefund() and ReverseRefund(). Always in sync with RefundRecords.
    /// </summary>
    public decimal TotalRefundedAmount { get; private set; }

    /// <summary>Computed: GrossAmount - TotalRefundedAmount. How much can still be refunded.</summary>
    public decimal RemainingRefundableAmount => GrossAmount - TotalRefundedAmount;

    // ── Gateway ───────────────────────────────────────────────────────────────
    public PaymentTransactionStatus Status        { get; private set; }
    public string  GatewayProvider                { get; private set; } = default!;
    public string? GatewayReference               { get; private set; }
    public string  IdempotencyKey                 { get; private set; } = default!;
    public bool    EscrowRequired                 { get; private set; }

    // ── Capture / Release lifecycle ───────────────────────────────────────────
    public DateTime? CapturedAt  { get; private set; }
    public DateTime? ReleasedAt  { get; private set; }

    // ── Cancellation lifecycle ────────────────────────────────────────────────
    public DateTime?           CancelledAt         { get; private set; }
    public CancellationReason? CancellationReason  { get; private set; }

    /// <summary>When a Cancelled transaction was restored to PendingIntent.</summary>
    public DateTime? ReinstatedAt    { get; private set; }
    public string?   ReinstationNote { get; private set; }

    // ── Refund lifecycle ──────────────────────────────────────────────────────

    /// <summary>Timestamp of the most recent applied refund record. Full history → RefundRecords.</summary>
    public DateTime? LastRefundedAt { get; private set; }

    // ── Dispute lifecycle ─────────────────────────────────────────────────────
    public DateTime? DisputedAt        { get; private set; }
    public string?   DisputeResolution { get; private set; }

    // ── Admin ─────────────────────────────────────────────────────────────────
    public string? AdminNote { get; private set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    private readonly List<TransactionRefundRecord> _refundRecords = [];
    public IReadOnlyCollection<TransactionRefundRecord> RefundRecords => _refundRecords.AsReadOnly();

    private PaymentTransactionEntity() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static PaymentTransactionEntity Create(
        string transactionCode,
        TransactionType transactionType,
        TransactionContextType contextType,
        long contextId,
        long? contextSubId,
        long payerProfileId,
        long? recipientProfileId,
        decimal grossAmount,
        decimal commissionAmount,
        decimal commissionRateSnapshot,
        decimal vatOnCommission,
        decimal netPayoutAmount,
        decimal discountAmount,
        string currencyCode,
        string gatewayProvider,
        string idempotencyKey,
        bool escrowRequired)
    {
        return new PaymentTransactionEntity
        {
            TransactionCode        = transactionCode,
            TransactionType        = transactionType,
            ContextType            = contextType,
            ContextId              = contextId,
            ContextSubId           = contextSubId,
            PayerProfileId         = payerProfileId,
            RecipientProfileId     = recipientProfileId,
            GrossAmount            = grossAmount,
            CommissionAmount       = commissionAmount,
            CommissionRateSnapshot = commissionRateSnapshot,
            VatOnCommission        = vatOnCommission,
            NetPayoutAmount        = netPayoutAmount,
            DiscountAmount         = discountAmount,
            CurrencyCode           = currencyCode.ToUpperInvariant(),
            Status                 = PaymentTransactionStatus.PendingIntent,
            GatewayProvider        = gatewayProvider,
            IdempotencyKey         = idempotencyKey,
            EscrowRequired         = escrowRequired,
            TotalRefundedAmount    = 0m,
            IsActive               = true,
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Domain transitions
    // ═══════════════════════════════════════════════════════════════════════════

    // ── Capture ───────────────────────────────────────────────────────────────

    /// <summary>Money collected by the gateway. PendingIntent → Captured.</summary>
    public void Capture(string? gatewayReference)
    {
        if (Status != PaymentTransactionStatus.PendingIntent)
            throw new AizenBusinessException((int)PaymentErrorCode.TransactionInvalidState);

        Status           = PaymentTransactionStatus.Captured;
        GatewayReference = gatewayReference;
        CapturedAt       = DateTime.UtcNow;
    }

    // ── Release ───────────────────────────────────────────────────────────────

    /// <summary>Escrow released to provider after SR completion. Captured → Released.</summary>
    public void Release(string? adminNote = null)
    {
        if (Status != PaymentTransactionStatus.Captured)
            throw new AizenBusinessException((int)PaymentErrorCode.TransactionInvalidState);

        Status     = PaymentTransactionStatus.Released;
        ReleasedAt = DateTime.UtcNow;
        AdminNote  = adminNote;
    }

    // ── Cancel (PendingIntent only) ───────────────────────────────────────────

    /// <summary>
    /// Cancels a PendingIntent transaction before any money is captured.
    /// No gateway call required — intent is voided in-system.
    ///
    /// For Captured transactions: issue a full refund via ApplyRefund() instead.
    /// </summary>
    public void Cancel(CancellationReason reason, string? adminNote = null)
    {
        if (Status != PaymentTransactionStatus.PendingIntent)
            throw new AizenBusinessException((int)PaymentErrorCode.TransactionInvalidState);

        Status             = PaymentTransactionStatus.Cancelled;
        CancelledAt        = DateTime.UtcNow;
        CancellationReason = reason;
        if (adminNote is not null) AdminNote = adminNote;
    }

    // ── Reinstate Cancellation ────────────────────────────────────────────────

    /// <summary>
    /// Restores a Cancelled transaction back to PendingIntent.
    /// Use when a cancellation was made in error and the payer still wants to pay.
    ///
    /// After reinstatement: a new checkout must be initiated (new GatewayReference).
    /// Audit: CancelledAt + CancellationReason remain set for history.
    /// ReinstatedAt marks the restoration timestamp.
    /// </summary>
    public void ReinstateCancellation(string adminNote)
    {
        if (Status != PaymentTransactionStatus.Cancelled)
            throw new AizenBusinessException((int)PaymentErrorCode.TransactionInvalidState);

        if (string.IsNullOrWhiteSpace(adminNote))
            throw new ArgumentException("Admin note is required when reinstating a cancelled transaction.", nameof(adminNote));

        Status          = PaymentTransactionStatus.PendingIntent;
        ReinstatedAt    = DateTime.UtcNow;
        ReinstationNote = adminNote;
    }

    // ── Apply Refund ──────────────────────────────────────────────────────────

    /// <summary>
    /// Applies a gateway-confirmed refund record to this transaction.
    /// Updates TotalRefundedAmount and recalculates Status.
    ///
    /// Must be called AFTER the TransactionRefundRecord is marked as Processed
    /// (i.e., after the gateway confirms the refund).
    ///
    /// Supported source states: Captured | Released | PartiallyRefunded
    /// </summary>
    public void ApplyRefund(TransactionRefundRecord refundRecord)
    {
        if (refundRecord.PaymentTransactionId != Id)
            throw new AizenBusinessException((int)PaymentErrorCode.TransactionInvalidState);

        if (refundRecord.Status != TransactionRefundStatus.Processed)
            throw new AizenBusinessException((int)PaymentErrorCode.RefundRecordInvalidState);

        var validStatuses = new[]
        {
            PaymentTransactionStatus.Captured,
            PaymentTransactionStatus.Released,
            PaymentTransactionStatus.PartiallyRefunded,
        };

        if (!validStatuses.Contains(Status))
            throw new AizenBusinessException((int)PaymentErrorCode.TransactionInvalidState);

        if (refundRecord.Amount > RemainingRefundableAmount + 0.001m) // tolerance for decimal precision
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAmountExceedsMaximum);

        if (!_refundRecords.Any(r => r.Id == refundRecord.Id && r.Id != 0))
            _refundRecords.Add(refundRecord);

        TotalRefundedAmount += refundRecord.Amount;
        LastRefundedAt       = DateTime.UtcNow;

        Status = RemainingRefundableAmount <= 0.001m
            ? PaymentTransactionStatus.Refunded
            : PaymentTransactionStatus.PartiallyRefunded;
    }

    // ── Reverse Refund ────────────────────────────────────────────────────────

    /// <summary>
    /// Reverses a previously applied partial (or full) refund.
    /// Subtracts the reversed amount from TotalRefundedAmount and recalculates Status.
    ///
    /// Use cases:
    ///   - Admin issued a refund in error before bank settlement.
    ///   - Partial refund was applied to the wrong transaction.
    ///
    /// Post-reversal status:
    ///   TotalRefunded = 0 → Captured (if not released) or Released (if escrow already released)
    ///   TotalRefunded > 0 → PartiallyRefunded
    ///
    /// The refund record is marked as Reversed (not deleted) for full audit trail.
    /// </summary>
    public void ReverseRefund(
        TransactionRefundRecord refundRecord,
        string reversalReason,
        string? adminNote = null)
    {
        if (refundRecord.PaymentTransactionId != Id)
            throw new AizenBusinessException((int)PaymentErrorCode.TransactionInvalidState);

        // Delegate state change to the record — throws if not Processed
        refundRecord.Reverse(reversalReason, adminNote);

        TotalRefundedAmount = Math.Max(0m, TotalRefundedAmount - refundRecord.Amount);

        // Recalculate transaction status
        Status = TotalRefundedAmount <= 0.001m
            ? (ReleasedAt.HasValue
                ? PaymentTransactionStatus.Released
                : PaymentTransactionStatus.Captured)
            : PaymentTransactionStatus.PartiallyRefunded;

        if (adminNote is not null) AdminNote = adminNote;
    }

    // ── Dispute ───────────────────────────────────────────────────────────────

    public void Dispute()
    {
        if (Status != PaymentTransactionStatus.Captured && Status != PaymentTransactionStatus.Released)
            throw new AizenBusinessException((int)PaymentErrorCode.TransactionInvalidState);

        Status     = PaymentTransactionStatus.Disputed;
        DisputedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resolves a dispute. Transaction returns to its pre-dispute state.
    /// If resolution favours the payer, follow with ApplyRefund().
    /// </summary>
    public void ResolveDispute(string resolution)
    {
        if (Status != PaymentTransactionStatus.Disputed)
            throw new AizenBusinessException((int)PaymentErrorCode.TransactionInvalidState);

        DisputeResolution = resolution;
        Status = ReleasedAt.HasValue
            ? PaymentTransactionStatus.Released
            : PaymentTransactionStatus.Captured;
    }

    // ── Failure ───────────────────────────────────────────────────────────────

    public void MarkFailed()
    {
        Status = PaymentTransactionStatus.Failed;
    }
}
