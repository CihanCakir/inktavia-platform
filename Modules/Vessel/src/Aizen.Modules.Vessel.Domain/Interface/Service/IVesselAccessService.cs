using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Domain.Interface.Service;

[DocumentationInfo("Vessel access service interface", "Checks user access rights and visibility rules for a vessel.")]
public interface IVesselAccessService
{
    Task<bool> UserHasAccessAsync(long vesselId, long userId, CancellationToken cancellationToken = default);
    Task<bool> UserHasRoleAsync(long vesselId, long userId, VesselOwnershipRole role, CancellationToken cancellationToken = default);
    Task<bool> CanViewVesselAsync(long vesselId, long? requestingUserId, CancellationToken cancellationToken = default);
}
