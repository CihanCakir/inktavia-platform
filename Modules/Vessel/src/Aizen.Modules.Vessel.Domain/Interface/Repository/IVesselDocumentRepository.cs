using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;

namespace Aizen.Modules.Vessel.Domain.Interface.Repository;

[DocumentationInfo("Vessel document repository interface", "Data access contract for vessel official documents.")]
public interface IVesselDocumentRepository
{
    Task<VesselDocumentEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VesselDocumentEntity>> GetByVesselIdAsync(long vesselId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VesselDocumentEntity>> GetExpiringAsync(int daysAhead, CancellationToken cancellationToken = default);
    Task AddAsync(VesselDocumentEntity entity, CancellationToken cancellationToken = default);
    void Update(VesselDocumentEntity entity);
}
