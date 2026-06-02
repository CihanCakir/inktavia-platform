using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;

namespace Aizen.Modules.Vessel.Domain.Interface.Repository;

[DocumentationInfo("Vessel repository interface", "Data access contract for the vessel root aggregate.")]
public interface IVesselRepository
{
    Task<VesselEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<VesselEntity?> GetByIdWithDetailsAsync(long id, CancellationToken cancellationToken = default);
    Task<VesselEntity?> GetByCodeAsync(string vesselCode, CancellationToken cancellationToken = default);
    Task<VesselEntity?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string vesselCode, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VesselEntity>> GetByOwnerUserIdAsync(long userId, CancellationToken cancellationToken = default);
    Task AddAsync(VesselEntity entity, CancellationToken cancellationToken = default);
    void Update(VesselEntity entity);
}
