namespace Aizen.Modules.ReferenceData.Abstraction.Request.LookupItem;

public sealed class CreateLookupItemRequest
{
    public string GroupCode { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? IconKey { get; set; }
    public string? ColorCode { get; set; }
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
}
