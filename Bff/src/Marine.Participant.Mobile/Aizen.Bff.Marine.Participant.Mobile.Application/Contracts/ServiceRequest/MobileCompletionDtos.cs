namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;

// BE_MO4 — owner completion review. The owner reviews the provider's completion (notes + evidence), sees the
// auto-approve countdown (N3), and approves (→ SR Completed → the existing decoupled escrow release) or rejects
// (N-E reason + note). COST-FREE: only the completion status/notes/evidence + review fields cross — never any
// commission / cost / provider net. Enums cross as their string names; the provider/reviewer user ids are dropped.

/// <summary>The provider's completion submission the owner is reviewing (cost-free). Present only when a completion
/// exists on the SR; <see cref="IsPendingReview"/> is true while it still awaits the owner's decision.</summary>
public sealed class MobileServiceRequestCompletionDto
{
    public long CompletionId { get; set; }
    public long ServiceRequestId { get; set; }
    /// <summary>Completion status name (Submitted/ApprovedByOwner/RejectedByOwner/DisputedByOwner/ClosedByAdmin).</summary>
    public string Status { get; set; } = default!;
    /// <summary>The provider's completion notes (what was done).</summary>
    public string? CompletionNotes { get; set; }

    /// <summary>The single evidence file's id (reference only). The viewable URL is <see cref="EvidenceUrl"/>.</summary>
    public Guid? EvidenceFileId { get; set; }
    /// <summary>A freshly-minted presigned read URL for the evidence (best-effort — null if it could not be resolved;
    /// the FE shows a graceful placeholder). Shares the attachment read-url path/fix.</summary>
    public string? EvidenceUrl { get; set; }
    public DateTime? EvidenceUrlExpiresAt { get; set; }

    public DateTime SubmittedAt { get; set; }

    // ── N3 auto-approval — the owner's countdown is derived from AutoApproveAt ──
    /// <summary>UTC deadline after which a still-pending completion is auto-approved. Null ⇒ no auto-approval
    /// scheduled (the FE hides the countdown). The FE renders "…{tarih} tarihinde otomatik onaylanacak ({n} gün)".</summary>
    public DateTime? AutoApproveAt { get; set; }

    // ── owner review outcome (present once the owner acted) ──
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    /// <summary>N-E structured reject reason name (WorkIncomplete/QualityIssue/NotAsAgreed/Other); null unless rejected.</summary>
    public string? RejectReasonCode { get; set; }
    /// <summary>Owner satisfaction rating (1..5) captured at approval; null when not rated.</summary>
    public int? ClientRating { get; set; }

    /// <summary>True while the completion is Submitted and awaiting the owner's approve/reject decision (drives the
    /// review section's visibility + the action CTAs). False once acted on (approved/rejected/disputed/closed).</summary>
    public bool IsPendingReview { get; set; }
}

/// <summary>Approve payload — an optional owner satisfaction rating (1..5) + an optional note. Approving transitions
/// the SR to Completed and lets the existing decoupled escrow release run; no amounts are client-supplied.</summary>
public sealed class ApproveMobileCompletionRequest
{
    /// <summary>Optional 1..5 satisfaction rating. Omit to approve without rating.</summary>
    public int? Rating { get; set; }
    public string? Note { get; set; }
}

/// <summary>Reject payload — the N-E structured reason (enum name) + an optional note. Rejecting sends the SR back to
/// InProgress; no money moves.</summary>
public sealed class RejectMobileCompletionRequest
{
    /// <summary>CompletionRejectReason name (WorkIncomplete/QualityIssue/NotAsAgreed/Other). Blank → Other.</summary>
    public string? ReasonCode { get; set; }
    public string? Note { get; set; }
}
