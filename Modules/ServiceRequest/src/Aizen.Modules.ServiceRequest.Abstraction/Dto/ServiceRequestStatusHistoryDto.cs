using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest status history DTO", "Represents a single status transition in the service request lifecycle.")]
public sealed class ServiceRequestStatusHistoryDto
{
    public long Id { get; set; }
    public ServiceRequestStatus FromStatus { get; set; }
    public ServiceRequestStatus ToStatus { get; set; }
    public string? Reason { get; set; }
    public long? ActorUserId { get; set; }
    public ServiceRequestActorType ActorType { get; set; }
    public DateTime OccurredAt { get; set; }
}
