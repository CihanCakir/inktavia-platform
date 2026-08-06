using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Completion;

[DocumentationInfo("ServiceRequest completion entity", "Completion evidence submitted by provider, pending owner approval.")]
public sealed class ServiceRequestCompletionEntity : AizenEntityWithAudit
{
    public long ServiceRequestId { get; private set; }
    public long ServiceRequestAssignmentId { get; private set; }
    public long ProviderUserId { get; private set; }
    public ServiceRequestCompletionStatus Status { get; private set; }
    public string? CompletionNotes { get; private set; }
    public Guid? EvidenceFileId { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public long? ReviewedByUserId { get; private set; }
    public string? ReviewNotes { get; private set; }
    /// <summary>N-E structured reason when the owner rejects the completion. Null otherwise / for pre-taxonomy rows.</summary>
    public CompletionRejectReason? RejectReasonCode { get; private set; }
    public int? ClientRating { get; private set; }

    // ── N3-C — completion auto-approval (append-only; UTC) ──
    /// <summary>UTC deadline after which a still-pending completion is auto-approved (= SubmittedAt + AutoApproveWindowDays).
    /// Null for rows submitted before N3-C (never auto-approved).</summary>
    public DateTime? AutoApproveAt { get; private set; }
    /// <summary>UTC time the "approaching" reminder was sent — the once-guard so the reminder never re-fires.</summary>
    public DateTime? AutoApproveReminderSentAt { get; private set; }

    public ServiceRequestCompletionEntity() { }

    public static ServiceRequestCompletionEntity Create(
        long serviceRequestId,
        long serviceRequestAssignmentId,
        long providerUserId,
        string? completionNotes,
        Guid? evidenceFileId)
    {
        return new ServiceRequestCompletionEntity
        {
            ServiceRequestId = serviceRequestId,
            ServiceRequestAssignmentId = serviceRequestAssignmentId,
            ProviderUserId = providerUserId,
            Status = ServiceRequestCompletionStatus.Submitted,
            CompletionNotes = completionNotes,
            EvidenceFileId = evidenceFileId,
            SubmittedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void ApproveByOwner(long reviewerUserId, string? reviewNotes)
    {
        Status = ServiceRequestCompletionStatus.ApprovedByOwner;
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = reviewerUserId;
        ReviewNotes = reviewNotes;
    }

    public void RejectByOwner(long reviewerUserId, string? reviewNotes, CompletionRejectReason? reasonCode = null)
    {
        Status = ServiceRequestCompletionStatus.RejectedByOwner;
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = reviewerUserId;
        ReviewNotes = reviewNotes;
        RejectReasonCode = reasonCode;
    }

    public void DisputeByOwner(long reviewerUserId, string? reviewNotes)
    {
        Status = ServiceRequestCompletionStatus.DisputedByOwner;
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = reviewerUserId;
        ReviewNotes = reviewNotes;
    }

    public void RateByClient(int rating)
    {
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "Client rating must be between 1 and 5.");

        ClientRating = rating;
    }

    /// <summary>N3-C — freeze the auto-approval deadline at submission (SubmittedAt + window). UTC.</summary>
    public void ScheduleAutoApproval(DateTime autoApproveAtUtc) => AutoApproveAt = autoApproveAtUtc;

    /// <summary>N3-C — stamp that the approaching reminder was sent (once-guard).</summary>
    public void MarkAutoApproveReminderSent() => AutoApproveReminderSentAt = DateTime.UtcNow;
}
