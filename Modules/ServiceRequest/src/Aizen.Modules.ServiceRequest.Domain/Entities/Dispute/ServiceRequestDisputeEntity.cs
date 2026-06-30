using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;

[DocumentationInfo("ServiceRequest dispute entity", "Dispute opened by owner, provider, or admin when completion is contested.")]
public sealed class ServiceRequestDisputeEntity : AizenEntityWithAudit
{
    public long ServiceRequestId { get; private set; }
    public long OpenedByUserId { get; private set; }
    public ServiceRequestActorType OpenedByActorType { get; private set; }
    public ServiceRequestDisputeStatus Status { get; private set; }
    public ServiceRequestDisputeReason Reason { get; private set; }
    public string Description { get; private set; } = default!;
    public string? ResolutionNotes { get; private set; }
    public long? ResolvedByAdminUserId { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime OpenedAt { get; private set; }

    public ServiceRequestDisputeEntity() { }

    public static ServiceRequestDisputeEntity Create(
        long serviceRequestId,
        long openedByUserId,
        ServiceRequestActorType openedByActorType,
        ServiceRequestDisputeReason reason,
        string description)
    {
        return new ServiceRequestDisputeEntity
        {
            ServiceRequestId = serviceRequestId,
            OpenedByUserId = openedByUserId,
            OpenedByActorType = openedByActorType,
            Status = ServiceRequestDisputeStatus.Open,
            Reason = reason,
            Description = description.Trim(),
            OpenedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void ChangeStatus(ServiceRequestDisputeStatus newStatus) => Status = newStatus;

    public void Resolve(long adminUserId, string? resolutionNotes)
    {
        Status = ServiceRequestDisputeStatus.Resolved;
        ResolutionNotes = resolutionNotes;
        ResolvedByAdminUserId = adminUserId;
        ResolvedAt = DateTime.UtcNow;
    }
}
