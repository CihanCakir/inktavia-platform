using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;

public sealed class CreateLookupGroupRequest
{
    public long? ParentLookupGroupId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public LookupGroupType GroupType { get; set; }
    public int SortOrder { get; set; }
    public bool IsSystemGroup { get; set; }
}
