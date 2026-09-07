using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;

public sealed class LookupItemDto
{
    public long Id { get; set; }
    public long LookupGroupId { get; set; }
    public string GroupCode { get; set; } = default!;
    public string? GroupName { get; set; }
    public LookupGroupType GroupType { get; set; }
    public string? GroupHierarchyPath { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    /// <summary>Turkish display name (additive; null → client falls back to <see cref="Name"/>). Client picks by language.</summary>
    public string? DisplayNameTr { get; set; }
    public string? Description { get; set; }
    public string? IconKey { get; set; }
    public string? ColorCode { get; set; }
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}
