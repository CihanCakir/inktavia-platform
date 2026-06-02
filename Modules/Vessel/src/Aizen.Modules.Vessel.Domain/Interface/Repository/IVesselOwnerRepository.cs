using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;

namespace Aizen.Modules.Vessel.Domain.Interface.Repository;

[DocumentationInfo("Vessel owner repository interface", "Data access contract for vessel ownership records.")]
public interface IVesselOwnerRepository
{
    Task<VesselOwnerEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<VesselOwnerEntity?> GetByVesselAndUserAsync(long vesselId, long userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VesselOwnerEntity>> GetByVesselIdAsync(long vesselId, CancellationToken cancellationToken = default);
    Task<VesselOwnerEntity?> GetPrimaryOwnerAsync(long vesselId, CancellationToken cancellationToken = default);
    Task<bool> UserHasRoleAsync(long vesselId, long userId, VesselOwnershipRole role, CancellationToken cancellationToken = default);
    Task AddAsync(VesselOwnerEntity entity, CancellationToken cancellationToken = default);
    void Update(VesselOwnerEntity entity);
}
