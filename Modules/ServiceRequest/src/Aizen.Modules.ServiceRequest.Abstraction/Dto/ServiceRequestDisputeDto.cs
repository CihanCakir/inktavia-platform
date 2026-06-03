using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest dispute DTO", "Represents an active or resolved dispute on a service request.")]
public sealed class ServiceRequestDisputeDto
{
    public long Id { get; set; }
    public long ServiceRequestId { get; set; }
    public long OpenedByUserId { get; set; }
    public ServiceRequestActorType OpenedByActorType { get; set; }
    public ServiceRequestDisputeStatus Status { get; set; }
    public ServiceRequestDisputeReason Reason { get; set; }
    public string Description { get; set; } = default!;
    public string? ResolutionNotes { get; set; }
    public long? ResolvedByAdminUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime OpenedAt { get; set; }
}
