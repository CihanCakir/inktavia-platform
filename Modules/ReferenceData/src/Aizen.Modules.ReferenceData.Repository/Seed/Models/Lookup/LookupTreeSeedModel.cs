
namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.Lookup;

/// <summary>Hierarchical tree seed model representing the Inktavia Marine OS lookup structure.</summary>
[DocumentationInfo("Hierarchical tree model for documenting the lookup group tree structure.", "Used by lookup-tree-marine.json. Not directly processed by the seed service; lookup-groups.json is authoritative.")]
public sealed class LookupTreeSeedModel
{
    public string Code { get; set; } = default!;
    public string? Name { get; set; }
    public List<LookupTreeSeedModel> Children { get; set; } = new();
}
