using Aizen.Core.Domain;
using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Modules.Vessel.Domain.Entities.Vessel;

[DocumentationInfo("Vessel status history entity", "Records each status transition of a vessel for audit and timeline purposes.")]
public sealed class VesselStatusHistoryEntity : AizenEntityWithAudit
{
    public long VesselId { get; private set; }
    public VesselStatus? FromStatus { get; private set; }
    public VesselStatus ToStatus { get; private set; }
    public string? Reason { get; private set; }
    public long? ChangedByUserId { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public VesselEntity? Vessel { get; private set; }

    public VesselStatusHistoryEntity() { }

    public static VesselStatusHistoryEntity Create(
        long vesselId, VesselStatus? fromStatus, VesselStatus toStatus,
        string? reason, long? changedByUserId)
    {
        return new VesselStatusHistoryEntity
        {
            VesselId = vesselId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Reason = reason,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
            IsActive = true
        };
    }
}
