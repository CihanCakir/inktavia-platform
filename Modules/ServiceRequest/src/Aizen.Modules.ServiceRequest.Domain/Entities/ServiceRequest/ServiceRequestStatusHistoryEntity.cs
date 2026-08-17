using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;

[DocumentationInfo("ServiceRequest status history entity", "Immutable audit trail of status transitions for a service request.")]
public sealed class ServiceRequestStatusHistoryEntity : AizenEntityWithAudit
{
    public long ServiceRequestId { get; private set; }
    public ServiceRequestStatus FromStatus { get; private set; }
    public ServiceRequestStatus ToStatus { get; private set; }
    public string? Reason { get; private set; }
    public long? ActorUserId { get; private set; }
    public ServiceRequestActorType ActorType { get; private set; }
    public DateTime OccurredAt { get; private set; }

    public ServiceRequestEntity ServiceRequest { get; private set; } = default!;

    public ServiceRequestStatusHistoryEntity() { }

    public static ServiceRequestStatusHistoryEntity Create(
        long serviceRequestId,
        ServiceRequestStatus fromStatus,
        ServiceRequestStatus toStatus,
        string? reason,
        long? actorUserId,
        ServiceRequestActorType actorType)
    {
        return new ServiceRequestStatusHistoryEntity
        {
            ServiceRequestId = serviceRequestId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Reason = reason,
            ActorUserId = actorUserId,
            ActorType = actorType,
            OccurredAt = DateTime.UtcNow,
            IsActive = true
        };
    }
}
