namespace Aizen.Modules.Vessel.Abstraction.Dto.CatalogReference;

/// <summary>Which catalog FK column a count/repoint targets. The Vessel module owns these soft-reference columns
/// (vessel_specifications.VesselBrandId/VesselModelId, vessel_engines.EngineBrandId/EngineModelId).</summary>
public enum CatalogRefKind
{
    VesselBrand = 1,
    VesselModel = 2,
    EngineBrand = 3,
    EngineModel = 4,
}

/// <summary>Grouped "how many vessels reference each catalog id" counts, per kind. Keyed by catalog id → count.
/// Consumed by the admin catalog review screen (ReferenceData review list, stitched at the BFF).</summary>
public sealed class CatalogReferenceCountsDto
{
    public Dictionary<long, int> VesselBrand { get; set; } = new();
    public Dictionary<long, int> VesselModel { get; set; } = new();
    public Dictionary<long, int> EngineBrand { get; set; } = new();
    public Dictionary<long, int> EngineModel { get; set; } = new();
}

/// <summary>Result of a merge repoint: how many vessel rows were moved from source id → target id.</summary>
public sealed class CatalogRepointResultDto
{
    public int RepointedCount { get; set; }
}
