using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

namespace Aizen.Modules.Vessel.Domain.Interface.Service;

[DocumentationInfo("Vessel snapshot service interface", "Builds denormalized VesselDetailDto and maintains the MongoDB read-side snapshot.")]
public interface IVesselSnapshotService
{
    Task<VesselDetailDto?> BuildDetailAsync(long vesselId, CancellationToken cancellationToken = default);
    Task SyncReadDocumentAsync(long vesselId, CancellationToken cancellationToken = default);
}
