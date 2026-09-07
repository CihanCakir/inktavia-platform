using Aizen.Modules.Vessel.Abstraction.Dto.CatalogReference;

namespace Aizen.Modules.Vessel.Abstraction.Request.CatalogReference;

/// <summary>Repoint every vessel reference from a source catalog id to a target id (merge of duplicate catalog entries).</summary>
public sealed class RepointCatalogReferenceRequest
{
    public CatalogRefKind Kind { get; set; }
    public long SourceId { get; set; }
    public long TargetId { get; set; }
}
