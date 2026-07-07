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
    public int? ClientRating { get; private set; }

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

    public void RejectByOwner(long reviewerUserId, string? reviewNotes)
    {
        Status = ServiceRequestCompletionStatus.RejectedByOwner;
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = reviewerUserId;
        ReviewNotes = reviewNotes;
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
}
