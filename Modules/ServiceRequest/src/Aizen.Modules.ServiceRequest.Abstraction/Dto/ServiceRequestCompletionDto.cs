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
}
