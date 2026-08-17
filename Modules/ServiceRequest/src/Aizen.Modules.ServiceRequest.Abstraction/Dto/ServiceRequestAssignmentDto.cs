using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest assignment DTO", "Represents a provider assignment created after offer acceptance.")]
public sealed class ServiceRequestAssignmentDto
{
    public long Id { get; set; }
    public long ServiceRequestId { get; set; }
    public long ServiceRequestOfferId { get; set; }
    public long ProviderProfileId { get; set; }
    public long ProviderUserId { get; set; }
    public long? AssignedTeamMemberId { get; set; }
    public ServiceRequestAssignmentStatus Status { get; set; }
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public string? ProviderNotes { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
