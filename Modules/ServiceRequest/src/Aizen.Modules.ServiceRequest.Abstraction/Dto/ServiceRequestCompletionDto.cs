using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest completion DTO", "Represents a provider completion submission pending owner review.")]
public sealed class ServiceRequestCompletionDto
{
    public long Id { get; set; }
    public long ServiceRequestId { get; set; }
    public long ServiceRequestAssignmentId { get; set; }
    public long ProviderUserId { get; set; }
    public ServiceRequestCompletionStatus Status { get; set; }
    public string? CompletionNotes { get; set; }
    public Guid? EvidenceFileId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public long? ReviewedByUserId { get; set; }
    public string? ReviewNotes { get; set; }
    public int? ClientRating { get; set; }
    /// <summary>N-E structured reason the owner rejected the completion with (null unless rejected). Additive read field.</summary>
    public CompletionRejectReason? RejectReasonCode { get; set; }

    // ── N3-C — completion auto-approval (append-only; UTC) ──
    /// <summary>UTC deadline after which a still-pending completion is auto-approved. Null for pre-N3-C rows
    /// (never auto-approved). Additive read field — the owner's countdown ("otomatik onaylanacak") is derived from it.</summary>
    public DateTime? AutoApproveAt { get; set; }
    /// <summary>UTC time the "approaching" auto-approval reminder was sent (once-guard). Null until sent.</summary>
    public DateTime? AutoApproveReminderSentAt { get; set; }
}
