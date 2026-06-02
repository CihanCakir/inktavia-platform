using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;

public sealed class UpdateLookupGroupRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public LookupGroupType GroupType { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
