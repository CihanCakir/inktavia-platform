namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;

// BE_MO5 — owner disputes (open + my-disputes list + read-only cost-free case). The owner OPENS a dispute (N-E
// structured reason + description) on their own SR, sees THEIR disputes, and reads the composed case (SR summary +
// customer-facing economics + evidence + messages + lifecycle timeline + resolution outcome). Resolution and
// status-change stay ADMIN-only — the owner is read-only here (N3 notifies them of the outcome).
// COST-FREE (§20.9): nothing here carries supplier cost / dealer margin / commission / provider-net, nor any
// provider/owner/reviewer/sender user ids. Enums cross as their string names; file ids are resolved to read URLs.

/// <summary>Open-a-dispute payload — the N-E structured reason (enum name) + a free-text description. The owner may
/// open only on their own SR (BFF owner-gated); the actor resolves to Owner module-side.</summary>
public sealed class OpenMobileDisputeRequest
{
    /// <summary>ServiceRequestDisputeReason name (QualityIssue/IncompleteWork/PricingDispute/TimelineIssue/
    /// DamageOccurred/NoShow/Other). Blank → Other.</summary>
    public string? ReasonCode { get; set; }
    /// <summary>What went wrong, in the owner's words.</summary>
    public string? Description { get; set; }
}

/// <summary>One dispute row for the owner's "my disputes" list. Cost-free — only the dispute + its SR header.</summary>
public sealed class MobileDisputeListItemDto
{
    public long DisputeId { get; set; }
    public long ServiceRequestId { get; set; }
    public string ServiceRequestCode { get; set; } = string.Empty;
    public string ServiceRequestTitle { get; set; } = string.Empty;
    public string? ServiceCategoryCode { get; set; }
    /// <summary>ServiceRequestDisputeStatus name (Open/UnderReview/PendingOwnerResponse/…/Resolved/Closed).</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>ServiceRequestDisputeReason name.</summary>
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>True while the dispute is still open/actionable (status not Resolved/Closed).</summary>
    public bool IsOpen { get; set; }
    /// <summary>True when this owner opened the dispute (vs the provider/admin).</summary>
    public bool OpenedByMe { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>The owner's paged disputes + a global open/actionable count (independent of the page filter).</summary>
public sealed class MobileDisputeListDto
{
    public List<MobileDisputeListItemDto> Items { get; set; } = new();
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int OpenCount { get; set; }
    public int TotalReturned => Items.Count;
}

// ── cost-free case view (read-only) ─────────────────────────────────────────────────────────────────

/// <summary>Compact SR header for the case (cost-free — no user ids, no payment transaction id).</summary>
public sealed class MobileDisputeCaseServiceRequestDto
{
    public long Id { get; set; }
    public string RequestCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    /// <summary>ServiceRequestStatus name.</summary>
    public string Status { get; set; } = default!;
    public string? ServiceCategoryCode { get; set; }
    /// <summary>The assigned provider's display name only (never the provider id).</summary>
    public string? AssignedProviderName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}

/// <summary>The N-E structured reason chain (all enum names) that led to the dispute.</summary>
public sealed class MobileDisputeCaseReasonDto
{
    public string DisputeReason { get; set; } = default!;
    public string? DisputeDescription { get; set; }
    public string? CancelReasonCode { get; set; }
    public string? CompletionRejectReasonCode { get; set; }
}

/// <summary>One lifecycle transition on the SR (the dispute's timeline).</summary>
public sealed class MobileDisputeCaseTimelineEventDto
{
    public string FromStatus { get; set; } = default!;
    public string ToStatus { get; set; } = default!;
    public string? Reason { get; set; }
    public string ActorType { get; set; } = default!;
    public DateTime OccurredAt { get; set; }
}

/// <summary>One offer line's customer-facing figures (cost-free — no commission / provider-net / cost / margin).</summary>
public sealed class MobileDisputeCaseEconomicsLineDto
{
    public string LineRef { get; set; } = default!;
    public string? ItemType { get; set; }
    public decimal GrossBeforeDiscount { get; set; }
    public decimal CustomerDiscountAmount { get; set; }
    public decimal LineVatAmount { get; set; }
    public decimal LineTotalAmount { get; set; }
}

/// <summary>The accepted offer's customer-facing economics (cost-free — commission / provider-net dropped for the
/// owner; only what the customer pays crosses).</summary>
public sealed class MobileDisputeCaseEconomicsDto
{
    public string CurrencyCode { get; set; } = "TRY";
    public decimal ServiceAmount { get; set; }
    public decimal CustomerTotalAmount { get; set; }
    public List<MobileDisputeCaseEconomicsLineDto> Lines { get; set; } = new();
}

/// <summary>The provider's completion + its evidence (read URL best-effort).</summary>
public sealed class MobileDisputeCaseCompletionDto
{
    public string Status { get; set; } = default!;
    public string? CompletionNotes { get; set; }
    public string? EvidenceUrl { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectReasonCode { get; set; }
    public int? ClientRating { get; set; }
}

/// <summary>A provider work-log entry (evidence trail; cost-free — no provider id).</summary>
public sealed class MobileDisputeCaseWorkLogDto
{
    public string LogType { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTime LoggedAt { get; set; }
}

/// <summary>A conversation message on the SR (evidence trail; cost-free — no sender id).</summary>
public sealed class MobileDisputeCaseMessageDto
{
    public string SenderType { get; set; } = default!;
    public string MessageType { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string? AttachmentUrl { get; set; }
    public DateTime? CreatedAt { get; set; }
}

/// <summary>A refund allocation, customer-facing (cost-free — provider-net / commission reversals dropped).</summary>
public sealed class MobileDisputeCaseRefundDto
{
    public string RefundReason { get; set; } = default!;
    public string? RefundCause { get; set; }
    public decimal Amount { get; set; }
    public string RefundStatus { get; set; } = default!;
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>The P10 payment / refund state, customer-facing (cost-free — provider-net / commission / chargeback
/// internals dropped).</summary>
public sealed class MobileDisputeCasePaymentStateDto
{
    public bool HasTransaction { get; set; }
    public string? Status { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public decimal GrossAmount { get; set; }
    public decimal TotalRefundedAmount { get; set; }
    public decimal RefundableAmount { get; set; }
    public bool EscrowReleased { get; set; }
    public DateTime? CapturedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public List<MobileDisputeCaseRefundDto> Refunds { get; set; } = new();
}

/// <summary>The full read-only dispute case for the owner. Cost-free composition of the dispute + SR summary +
/// customer-facing economics + evidence (completion / work-logs / messages) + the lifecycle timeline + the
/// resolution outcome (present once the admin resolves; the owner never resolves).</summary>
public sealed class MobileDisputeCaseDto
{
    public long DisputeId { get; set; }
    public long ServiceRequestId { get; set; }
    /// <summary>ServiceRequestDisputeStatus name.</summary>
    public string Status { get; set; } = default!;
    /// <summary>ServiceRequestDisputeReason name.</summary>
    public string Reason { get; set; } = default!;
    public string? Description { get; set; }
    public bool OpenedByMe { get; set; }
    public DateTime OpenedAt { get; set; }

    /// <summary>True once the dispute is Resolved (drives the "İtiraz çözüldü: {outcome}" surface).</summary>
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    /// <summary>DisputeResolutionOutcome name (FavorPayerFullRefund/…); null while unresolved.</summary>
    public string? ResolutionOutcome { get; set; }
    public string? ResolutionNotes { get; set; }
    public decimal? ResolutionRefundAmount { get; set; }

    public MobileDisputeCaseServiceRequestDto ServiceRequest { get; set; } = default!;
    public MobileDisputeCaseReasonDto ReasonDetail { get; set; } = default!;
    public List<MobileDisputeCaseTimelineEventDto> StatusTimeline { get; set; } = new();
    public MobileDisputeCaseEconomicsDto? Economics { get; set; }
    public MobileDisputeCaseCompletionDto? Completion { get; set; }
    public List<MobileDisputeCaseWorkLogDto> WorkLogs { get; set; } = new();
    public List<MobileDisputeCaseMessageDto> Messages { get; set; } = new();
    public MobileDisputeCasePaymentStateDto? PaymentState { get; set; }
}
