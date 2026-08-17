using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Modules.Vessel.Domain.Interface.Service;

[DocumentationInfo("Vessel status service interface", "Validates and applies vessel status transitions, recording history entries.")]
public interface IVesselStatusService
{
    Task ChangeStatusAsync(long vesselId, VesselStatus newStatus, string? reason, long? changedByUserId, CancellationToken cancellationToken = default);
    bool IsValidTransition(VesselStatus from, VesselStatus to);
}
