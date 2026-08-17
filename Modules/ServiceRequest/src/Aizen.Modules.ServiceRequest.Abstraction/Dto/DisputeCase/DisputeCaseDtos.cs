using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto.DisputeCase;

// ─────────────────────────────────────────────────────────────────────────────
// BE-S13a — Dispute Case aggregate DTOs. A pure, read-only composition of the whole
// case file so an admin adjudicates from one place. CONFIDENTIALITY (§20.9): none of
// these carry supplier cost / dealer margin — only the derived, cost-free economics
// that already leave Payment. Nothing here is ever mapped from a cost/margin field.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Compact SR header for the case file.</summary>
public sealed class DisputeCaseServiceRequestDto
{
    public long Id { get; set; }
    public string RequestCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public ServiceRequestStatus Status { get; set; }
    public string? ServiceCategoryCode { get; set; }
    public long OwnerUserId { get; set; }
    public long? ProviderUserId { get; set; }
    public string? AssignedProviderName { get; set; }
    public long? PaymentTransactionId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}

/// <summary>The structured reason chain that led to the dispute (N-E taxonomy) — codes + the dispute's own reason.</summary>
public sealed class DisputeCaseReasonDto
{
    public ServiceRequestDisputeReason DisputeReason { get; set; }
    public string? DisputeDescription { get; set; }
    /// <summary>N-E structured cancel reason on the SR (if the request was cancelled).</summary>
    public ServiceRequestCancelReason? CancelReasonCode { get; set; }
    /// <summary>N-E structured completion-reject reason (if a rejected completion led here).</summary>
    public CompletionRejectReason? CompletionRejectReasonCode { get; set; }
}

/// <summary>A provider work-log entry (evidence trail).</summary>
public sealed class DisputeCaseWorkLogDto
{
    public long Id { get; set; }
    public long ProviderUserId { get; set; }
    public ServiceRequestWorkLogType LogType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public Guid? AttachmentFileId { get; set; }
    public DateTime LoggedAt { get; set; }
}

/// <summary>Completion + its evidence (the provider's proof-of-work).</summary>
public sealed class DisputeCaseCompletionDto
{
    public ServiceRequestCompletionStatus Status { get; set; }
    public string? CompletionNotes { get; set; }
    public Guid? EvidenceFileId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public CompletionRejectReason? RejectReasonCode { get; set; }
    public int? ClientRating { get; set; }
}

/// <summary>A conversation message on the SR (evidence trail).</summary>
public sealed class DisputeCaseMessageDto
{
    public long Id { get; set; }
    public long SenderUserId { get; set; }
    public ServiceRequestMessageSenderType SenderType { get; set; }
    public ServiceRequestMessageType MessageType { get; set; }
    public string Content { get; set; } = default!;
    public Guid? AttachmentFileId { get; set; }
    public DateTime? CreatedAt { get; set; }
}

// ── Economics (mirrors the cost-free Payment S8 snapshot) ────────────────────

/// <summary>A single offer line's economics (cost-free — no supplier list price / dealer margin).</summary>
public sealed class DisputeCaseEconomicsLineDto
{
    public string LineRef { get; set; } = default!;
    public string? ItemType { get; set; }
    public decimal GrossBeforeDiscount { get; set; }
    public decimal CustomerDiscountAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal ProviderNetAmount { get; set; }
    public decimal LineVatAmount { get; set; }
    public decimal LineTotalAmount { get; set; }
}

/// <summary>The accepted offer's immutable economics (S8), cost-confidential fields excluded.</summary>
public sealed class DisputeCaseEconomicsDto
{
    public long SnapshotId { get; set; }
    public string SnapshotCode { get; set; } = default!;
    public string CurrencyCode { get; set; } = "TRY";
    public decimal ServiceAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal ProviderNetAmount { get; set; }
    public decimal PlatformFeeGrossAmount { get; set; }
    public decimal CustomerTotalAmount { get; set; }
    public List<DisputeCaseEconomicsLineDto> Lines { get; set; } = new();
}

// ── Payment / refund state (P10) ─────────────────────────────────────────────

/// <summary>One §7.5 nine-field refund allocation (the deterministic breakdown of a refund).</summary>
public sealed class DisputeCaseRefundAllocationDto
{
    public long RefundRecordId { get; set; }
    public string RefundCode { get; set; } = default!;
    public string RefundReason { get; set; } = default!;
    public string? RefundCause { get; set; }
    public decimal Amount { get; set; }
    public string RefundStatus { get; set; } = default!;
    public decimal ServiceRefundAmount { get; set; }
    public decimal ProviderNetReversalAmount { get; set; }
    public decimal CommissionRevenueReversalAmount { get; set; }
    public decimal PlatformFeeGrossRefundAmount { get; set; }
    public decimal PlatformAdvancedRefundAmount { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>A chargeback record against the transaction.</summary>
public sealed class DisputeCaseChargebackDto
{
    public string GatewayChargebackReference { get; set; } = default!;
    public decimal Amount { get; set; }
    public decimal ProviderRecoveredAmount { get; set; }
    public decimal RemainingNegativeBalance { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
}

/// <summary>The P10 escrow / settlement / refund state for the SR's transaction.</summary>
public sealed class DisputeCasePaymentStateDto
{
    public bool HasTransaction { get; set; }
    public long? TransactionId { get; set; }
    public string? TransactionCode { get; set; }
    public string? Status { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public decimal GrossAmount { get; set; }
    public decimal TotalRefundedAmount { get; set; }
    /// <summary>GrossAmount − TotalRefundedAmount — the ceiling for a dispute partial refund.</summary>
    public decimal RefundableAmount { get; set; }
    public bool EscrowReleased { get; set; }
    public DateTime? CapturedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public List<DisputeCaseRefundAllocationDto> RefundAllocations { get; set; } = new();
    public DisputeCaseChargebackDto? Chargeback { get; set; }
}
