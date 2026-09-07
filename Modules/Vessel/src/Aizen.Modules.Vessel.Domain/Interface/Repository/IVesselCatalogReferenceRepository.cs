using Aizen.Modules.Vessel.Abstraction.Dto.CatalogReference;

namespace Aizen.Modules.Vessel.Domain.Interface.Repository;

/// <summary>
/// Vessel-side access to the SOFT catalog reference columns the Vessel module owns
/// (vessel_specifications.VesselBrandId/VesselModelId, vessel_engines.EngineBrandId/EngineModelId).
/// Used by the admin catalog review (reference counts) + the catalog merge repoint.
/// </summary>
public interface IVesselCatalogReferenceRepository
{
    /// <summary>Grouped counts of vessels referencing each catalog id, per kind.</summary>
    Task<CatalogReferenceCountsDto> GetReferenceCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>Repoints every vessel reference from <paramref name="sourceId"/> to <paramref name="targetId"/>
    /// for the given kind, in one transaction. Returns the number of rows moved (0 when none — idempotent).</summary>
    Task<int> RepointAsync(CatalogRefKind kind, long sourceId, long targetId, CancellationToken cancellationToken = default);
}
