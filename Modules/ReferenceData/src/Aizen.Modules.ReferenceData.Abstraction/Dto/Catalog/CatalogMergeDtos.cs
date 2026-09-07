namespace Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;

/// <summary>Which catalog entity kind a merge targets.</summary>
public enum CatalogMergeType
{
    VesselBrand = 1,
    VesselModel = 2,
    EngineBrand = 3,
    EngineModel = 4,
}

/// <summary>Merge a duplicate catalog entry (source) into a surviving one (target). Same type required;
/// same brand required for models.</summary>
public sealed class MergeCatalogRequest
{
    public CatalogMergeType Type { get; set; }
    public long SourceId { get; set; }
    public long TargetId { get; set; }
}

/// <summary>Result of the catalog-side merge (source deactivated + audited). The Vessel-reference repoint count is
/// added by the BFF from the Vessel module (this module does not own the FK columns).</summary>
public sealed class CatalogMergeResultDto
{
    public bool Success { get; set; }
    public CatalogMergeType Type { get; set; }
    public long SourceId { get; set; }
    public long TargetId { get; set; }
    /// <summary>True when the source was already merged before — a safe idempotent re-merge (no-op).</summary>
    public bool AlreadyMerged { get; set; }
    /// <summary>Vessel references repointed source→target. Populated by the BFF (Vessel module); null from the module alone.</summary>
    public int? RepointedVesselReferences { get; set; }
}
