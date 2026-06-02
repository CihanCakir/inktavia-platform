using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;

public sealed class LookupGroupTreeDto
{
    public long Id { get; set; }
    public long? ParentLookupGroupId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public LookupGroupType GroupType { get; set; }
    public int Level { get; set; }
    public string HierarchyPath { get; set; } = default!;
    public int SortOrder { get; set; }
    public bool IsSystemGroup { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<LookupGroupTreeDto> Children { get; set; } = Array.Empty<LookupGroupTreeDto>();
    public IReadOnlyList<LookupItemDto> Items { get; set; } = Array.Empty<LookupItemDto>();
}
