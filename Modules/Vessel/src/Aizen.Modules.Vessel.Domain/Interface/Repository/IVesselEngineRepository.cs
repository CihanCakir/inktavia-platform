using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;

namespace Aizen.Modules.Vessel.Domain.Interface.Repository;

[DocumentationInfo("Vessel engine repository interface", "Data access contract for vessel engine records.")]
public interface IVesselEngineRepository
{
    Task<VesselEngineEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VesselEngineEntity>> GetByVesselIdAsync(long vesselId, CancellationToken cancellationToken = default);
    Task<VesselEngineEntity?> GetPrimaryEngineAsync(long vesselId, CancellationToken cancellationToken = default);
    Task AddAsync(VesselEngineEntity entity, CancellationToken cancellationToken = default);
    void Update(VesselEngineEntity entity);
}
