using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;

namespace Aizen.Modules.Vessel.Domain.Interface.Repository;

[DocumentationInfo("Vessel media repository interface", "Data access contract for vessel media files.")]
public interface IVesselMediaRepository
{
    Task<VesselMediaEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<VesselMediaEntity?> GetByIdWithVesselAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VesselMediaEntity>> GetByVesselIdAsync(long vesselId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VesselMediaEntity>> GetByVesselIdAsync(long vesselId, bool onlyActive, CancellationToken cancellationToken = default);
    Task<VesselMediaEntity?> GetCoverAsync(long vesselId, CancellationToken cancellationToken = default);
    Task UnsetAllCoversAsync(long vesselId, CancellationToken cancellationToken = default);
    Task AddAsync(VesselMediaEntity entity, CancellationToken cancellationToken = default);
    void Update(VesselMediaEntity entity);
    void UpdateRange(IEnumerable<VesselMediaEntity> entities);
}

