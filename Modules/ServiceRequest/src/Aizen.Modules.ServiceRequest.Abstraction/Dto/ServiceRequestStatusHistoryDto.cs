using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest status history DTO", "Represents a single status transition in the service request lifecycle.")]
public sealed class ServiceRequestStatusHistoryDto
{
    public long Id { get; set; }
    public ServiceRequestStatus FromStatus { get; set; }
    public ServiceRequestStatus ToStatus { get; set; }
    /// <summary>Stable machine-readable event code (e.g. SR_PUBLISHED) derived from From/To status — for client
    /// localization. Additive; <see cref="Reason"/> (English prose) stays for backward compatibility.</summary>
    public string EventCode { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public long? ActorUserId { get; set; }
    public ServiceRequestActorType ActorType { get; set; }
    public DateTime OccurredAt { get; set; }
}
