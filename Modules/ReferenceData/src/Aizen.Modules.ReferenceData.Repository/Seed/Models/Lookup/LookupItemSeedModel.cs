
namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.Lookup;

/// <summary>Seed model for a LookupItem entity, read from lookup-items.json.</summary>
[DocumentationInfo("Seed model representing a lookup item loaded from JSON.", "Maps to LookupItemEntity. Idempotency key: GroupCode + Code.")]
public sealed class LookupItemSeedModel
{
    public string GroupCode { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? IconKey { get; set; }
    public string? ColorCode { get; set; }
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}
