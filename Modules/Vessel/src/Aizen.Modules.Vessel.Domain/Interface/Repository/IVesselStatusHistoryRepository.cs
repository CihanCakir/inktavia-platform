using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;

namespace Aizen.Modules.Vessel.Domain.Interface.Repository;

[DocumentationInfo("Vessel status history repository interface", "Data access contract for vessel status change history.")]
public interface IVesselStatusHistoryRepository
{
    Task<IReadOnlyList<VesselStatusHistoryEntity>> GetByVesselIdAsync(long vesselId, CancellationToken cancellationToken = default);
    Task AddAsync(VesselStatusHistoryEntity entity, CancellationToken cancellationToken = default);
}
