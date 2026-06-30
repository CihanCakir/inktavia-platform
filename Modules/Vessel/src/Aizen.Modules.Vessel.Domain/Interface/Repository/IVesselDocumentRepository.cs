using Aizen.Modules.Vessel.Domain.Entities.Vessel;

namespace Aizen.Modules.Vessel.Domain.Interface.Repository;

[DocumentationInfo("Vessel document repository interface", "Data access contract for vessel official documents.")]
public interface IVesselDocumentRepository
{
    Task<VesselDocumentEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<VesselDocumentEntity?> GetByIdWithVesselAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VesselDocumentEntity>> GetByVesselIdAsync(long vesselId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VesselDocumentEntity>> GetByVesselIdAsync(long vesselId, bool onlyActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VesselDocumentEntity>> GetExpiringAsync(int daysAhead, CancellationToken cancellationToken = default);
    Task<bool> ExistsActiveDocumentTypeAsync(long vesselId, string documentTypeCode, CancellationToken cancellationToken = default);
    Task AddAsync(VesselDocumentEntity entity, CancellationToken cancellationToken = default);
    void Update(VesselDocumentEntity entity);
}

