using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Payout;

[DocumentationInfo("Payout record entity",
    "Tracks a pending or completed provider payout. In manual mode, admin confirms; in Iyzico mode, transfer is automatic.")]
public sealed class PayoutRecordEntity : AizenEntityWithAudit
{
    public long         ProviderProfileId       { get; private set; }

    /// <summary>
    /// FK to PaymentTransactionEntity. Null for CargoDry settlement payouts
    /// which do not originate from a buyer-payment transaction.
    /// Phase 4B (July 2026): made nullable to support CargoDry consignment payout preparation.
    /// </summary>
    public long?        PaymentTransactionId    { get; private set; }

    /// <summary>
    /// Cross-module source domain type. E.g. "CargoDrySettlement" for sell-through settlement payouts.
    /// Null for standard ServiceRequest payouts (backwards-compatible).
    /// Used with SourceId for idempotency and reverse-navigation.
    /// </summary>
    public string?      SourceType              { get; private set; }

    /// <summary>
    /// Cross-module source entity Id. E.g. CargoDrySellThroughSettlement.Id.
    /// Null for standard ServiceRequest payouts.
    /// </summary>
    public long?        SourceId                { get; private set; }

    /// <summary>Human-readable payout description for admin panel and statements.</summary>
    public string?      Description             { get; private set; }

    public decimal      Amount                  { get; private set; }
    public string       CurrencyCode            { get; private set; } = "TRY";
    public PayoutStatus Status                  { get; private set; }
    public string       GatewayProvider         { get; private set; } = default!;
    public string?      GatewayPayoutId         { get; private set; }  // Iyzico transfer reference
    public DateTime     RequestedAt             { get; private set; }
    public DateTime?    ProcessedAt             { get; private set; }
    public DateTime?    HeldAt                  { get; private set; }
    public string?      HoldReason              { get; private set; }
    public string?      FailureReason           { get; private set; }
    public string?      AdminNote               { get; private set; }

    // ── Phase 4D: Payout lifecycle audit fields ──────────────────────────────────
    /// <summary>UTC timestamp when admin approved this payout for disbursement. Phase 4D.</summary>
    public DateTime?    ApprovedAtUtc           { get; private set; }
    /// <summary>Admin user Id who approved the payout. Phase 4D.</summary>
    public long?        ApprovedByUserId        { get; private set; }
    /// <summary>UTC timestamp when the payout entered Processing state. Phase 4D.</summary>
    public DateTime?    ProcessingAtUtc         { get; private set; }
    /// <summary>Admin user Id who initiated processing. Phase 4D.</summary>
    public long?        ProcessingByUserId      { get; private set; }
    /// <summary>Admin user Id who confirmed manual completion. Phase 4D.</summary>
    public long?        CompletedByUserId       { get; private set; }
    /// <summary>UTC timestamp when the payout was marked Failed. Phase 4D.</summary>
    public DateTime?    FailedAtUtc             { get; private set; }
    /// <summary>Admin user Id who recorded the failure. Phase 4D.</summary>
    public long?        FailedByUserId          { get; private set; }

    // ── Phase L12: Payout receipt PDF file reference ─────────────────────────────
    public string?      ReceiptFileRef          { get; private set; }

    public void SetReceiptRef(string receiptFileRef) => ReceiptFileRef = receiptFileRef;

    private PayoutRecordEntity() { }

    public static PayoutRecordEntity Create(
        long providerProfileId, long paymentTransactionId,
        decimal amount, string currencyCode, string gatewayProvider)
    {
        return new PayoutRecordEntity
        {
            ProviderProfileId    = providerProfileId,
            PaymentTransactionId = paymentTransactionId,
            Amount               = amount,
            CurrencyCode         = currencyCode.ToUpperInvariant(),
            Status               = PayoutStatus.Pending,
            GatewayProvider      = gatewayProvider,
            RequestedAt          = DateTime.UtcNow,
            IsActive             = true,
        };
    }

    /// <summary>
    /// Creates a payout preparation record for a CargoDry sell-through settlement.
    /// Does not require a PaymentTransactionId — the settlement itself is the source.
    /// Phase 4B (July 2026). Does NOT execute any external payout.
    /// </summary>
    public static PayoutRecordEntity CreateForCargoDrySettlement(
        long    providerProfileId,
        long    settlementId,
        decimal amount,
        string  currencyCode,
        string  description)
    {
        return new PayoutRecordEntity
        {
            ProviderProfileId = providerProfileId,
            PaymentTransactionId = null,
            SourceType        = "CargoDrySettlement",
            SourceId          = settlementId,
            Description       = description,
            Amount            = amount,
            CurrencyCode      = currencyCode.ToUpperInvariant(),
            Status            = PayoutStatus.Pending,
            GatewayProvider   = "Manual",   // Phase 4B: manual disbursement pending Phase 4C Iyzico wiring
            RequestedAt       = DateTime.UtcNow,
            IsActive          = true,
        };
    }

    public void MarkCompleted(string? gatewayPayoutId, string? adminNote = null)
    {
        Status          = PayoutStatus.Completed;
        GatewayPayoutId = gatewayPayoutId;
        ProcessedAt     = DateTime.UtcNow;
        AdminNote       = adminNote;
    }

    public void MarkFailed(string reason)
    {
        Status        = PayoutStatus.Failed;
        FailureReason = reason;
        ProcessedAt   = DateTime.UtcNow;
    }

    public void MarkProcessing() => Status = PayoutStatus.Processing;

    /// <summary>
    /// Puts a Pending or Processing payout on administrative hold.
    /// Use ApproveManualPayout() to release from hold.
    /// </summary>
    public void Hold(string reason, string? adminNote = null)
    {
        Status     = PayoutStatus.OnHold;
        HoldReason = reason;
        HeldAt     = DateTime.UtcNow;
        AdminNote  = adminNote;
    }

    /// <summary>
    /// Releases a held payout and marks it as completed via manual disbursement.
    /// </summary>
    public void ApproveManualPayout(string gatewayPayoutId, string? adminNote = null)
    {
        Status          = PayoutStatus.Completed;
        GatewayPayoutId = gatewayPayoutId;
        ProcessedAt     = DateTime.UtcNow;
        AdminNote       = adminNote ?? AdminNote;
    }

    // ── Phase 4D: CargoDry settlement payout lifecycle methods ───────────────────

    /// <summary>
    /// Admin approves a Pending payout for disbursement.
    /// Transition: Pending → Approved.
    /// Phase 4D (July 2026).
    /// </summary>
    public void Approve(long approvedByUserId, DateTime approvedAtUtc, string? note = null)
    {
        if (Status != PayoutStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot approve payout {Id}: current status is {Status}. Expected Pending.");

        Status           = PayoutStatus.Approved;
        ApprovedAtUtc    = approvedAtUtc;
        ApprovedByUserId = approvedByUserId;
        if (note is not null) AdminNote = note;
    }

    /// <summary>
    /// Marks an Approved (or Pending) payout as actively Processing.
    /// Records the admin user and an optional external reference (e.g. bank instruction ID).
    /// Transition: Approved/Pending → Processing.
    /// Phase 4D (July 2026). Does NOT call any payment gateway.
    /// </summary>
    public void MarkProcessingByAdmin(
        long     processedByUserId,
        DateTime processingAtUtc,
        string?  externalReference = null,
        string?  note = null)
    {
        if (Status is not (PayoutStatus.Approved or PayoutStatus.Pending))
            throw new InvalidOperationException(
                $"Cannot mark payout {Id} as Processing: current status is {Status}. " +
                "Expected Approved or Pending.");

        Status              = PayoutStatus.Processing;
        ProcessingAtUtc     = processingAtUtc;
        ProcessingByUserId  = processedByUserId;
        if (externalReference is not null) GatewayPayoutId = externalReference;
        if (note is not null) AdminNote = note;
    }

    /// <summary>
    /// Admin confirms the manual disbursement has been executed.
    /// Transition: Processing/Approved/Pending → Completed.
    /// Records the completing admin, completion timestamp, and the manual payment reference.
    /// Phase 4D (July 2026). Does NOT call any payment gateway.
    /// </summary>
    public void MarkCompletedManual(
        long     completedByUserId,
        DateTime completedAtUtc,
        string   manualPaymentReference,
        string?  note = null)
    {
        if (Status is PayoutStatus.Completed)
            return; // idempotent — already completed

        if (Status is PayoutStatus.Cancelled or PayoutStatus.Failed)
            throw new InvalidOperationException(
                $"Cannot complete payout {Id}: it is in terminal status {Status}.");

        Status            = PayoutStatus.Completed;
        CompletedByUserId = completedByUserId;
        ProcessedAt       = completedAtUtc;
        GatewayPayoutId   = manualPaymentReference;
        if (note is not null) AdminNote = note;
    }

    /// <summary>
    /// Records a payout failure with the responsible admin, reason, and optional external reference.
    /// Transition: any non-Completed, non-Cancelled state → Failed.
    /// Phase 4D (July 2026). Does NOT call any payment gateway.
    /// </summary>
    public void MarkFailedByAdmin(
        long     failedByUserId,
        DateTime failedAtUtc,
        string   reason,
        string?  externalReference = null,
        string?  note = null)
    {
        if (Status is PayoutStatus.Completed)
            throw new InvalidOperationException(
                $"Cannot fail payout {Id}: it is already Completed.");

        Status          = PayoutStatus.Failed;
        FailedAtUtc     = failedAtUtc;
        FailedByUserId  = failedByUserId;
        FailureReason   = reason;
        if (externalReference is not null) GatewayPayoutId = externalReference;
        if (note is not null) AdminNote = note;
    }
}
