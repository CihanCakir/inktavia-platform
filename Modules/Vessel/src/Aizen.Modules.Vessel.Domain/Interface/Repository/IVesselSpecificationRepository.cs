using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;

namespace Aizen.Modules.Vessel.Domain.Interface.Repository;

[DocumentationInfo("Vessel specification repository interface", "Data access contract for vessel physical specifications.")]
public interface IVesselSpecificationRepository
{
    Task<VesselSpecificationEntity?> GetByVesselIdAsync(long vesselId, CancellationToken cancellationToken = default);
    Task AddAsync(VesselSpecificationEntity entity, CancellationToken cancellationToken = default);
    void Update(VesselSpecificationEntity entity);
    void Remove(VesselSpecificationEntity entity);
}
