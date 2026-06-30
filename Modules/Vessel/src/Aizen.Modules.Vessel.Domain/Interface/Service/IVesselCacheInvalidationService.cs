
namespace Aizen.Modules.Vessel.Domain.Interface.Service;

[DocumentationInfo("Vessel cache invalidation service interface", "Invalidates cached query results when vessel data changes.")]
public interface IVesselCacheInvalidationService
{
    Task InvalidateVesselAsync(long vesselId, CancellationToken cancellationToken = default);
    Task InvalidateVesselByCodeAsync(string vesselCode, CancellationToken cancellationToken = default);
    Task InvalidateUserVesselListAsync(long userId, CancellationToken cancellationToken = default);
    Task InvalidateOwnersAsync(long vesselId, CancellationToken cancellationToken = default);
    Task InvalidateSpecificationAsync(long vesselId, CancellationToken cancellationToken = default);
    Task InvalidateEnginesAsync(long vesselId, CancellationToken cancellationToken = default);
    Task InvalidateDocumentsAsync(long vesselId, CancellationToken cancellationToken = default);
    Task InvalidateMediaAsync(long vesselId, CancellationToken cancellationToken = default);
    Task InvalidateLocationAsync(long vesselId, CancellationToken cancellationToken = default);
    Task InvalidateStatusHistoryAsync(long vesselId, CancellationToken cancellationToken = default);
}
