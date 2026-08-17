
namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.Lookup;

/// <summary>Seed model for a LookupGroup entity, read from lookup-groups.json.</summary>
[DocumentationInfo("Seed model representing a lookup group loaded from JSON.", "Maps to LookupGroupEntity. Idempotency key: Code. ParentCode is resolved to ParentLookupGroupId at seed time.")]
public sealed class LookupGroupSeedModel
{
    public string Code { get; set; } = default!;
    public string? ParentCode { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int GroupType { get; set; }
    public int SortOrder { get; set; }
    public bool IsSystemGroup { get; set; }
    public bool IsActive { get; set; } = true;
}
