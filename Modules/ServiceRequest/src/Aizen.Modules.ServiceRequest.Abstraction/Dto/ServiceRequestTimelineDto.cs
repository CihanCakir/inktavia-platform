using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest timeline DTO", "Full ordered timeline of events for a service request.")]
public sealed class ServiceRequestTimelineDto
{
    public long ServiceRequestId { get; set; }
    public string RequestCode { get; set; } = default!;
    public List<ServiceRequestTimelineEntryDto> Entries { get; set; } = new();
}

[DocumentationInfo("ServiceRequest timeline entry DTO", "Single event entry in the service request timeline.")]
public sealed class ServiceRequestTimelineEntryDto
{
    public string EventType { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public long? ActorUserId { get; set; }
    public ServiceRequestActorType? ActorType { get; set; }
    public DateTime OccurredAt { get; set; }
}
