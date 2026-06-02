using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;

namespace Aizen.Modules.Vessel.Domain.Interface.Repository;

[DocumentationInfo("Vessel location snapshot repository interface", "Data access contract for vessel location snapshots.")]
public interface IVesselLocationSnapshotRepository
{
    Task<VesselLocationSnapshotEntity?> GetCurrentAsync(long vesselId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VesselLocationSnapshotEntity>> GetHistoryAsync(long vesselId, int limit, CancellationToken cancellationToken = default);
    Task AddAsync(VesselLocationSnapshotEntity entity, CancellationToken cancellationToken = default);
    void Update(VesselLocationSnapshotEntity entity);
}
