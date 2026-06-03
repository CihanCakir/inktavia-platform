using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest realtime event DTO", "Typed realtime event payload published via SignalR for service request lifecycle changes.")]
public sealed class ServiceRequestRealtimeEventDto
{
    public long ServiceRequestId { get; set; }
    public string RequestCode { get; set; } = default!;
    public ServiceRequestRealtimeEventType EventType { get; set; }
    public string? PayloadType { get; set; }
    public string? PayloadJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public long? ActorUserId { get; set; }
    public ServiceRequestActorType? ActorType { get; set; }
}
