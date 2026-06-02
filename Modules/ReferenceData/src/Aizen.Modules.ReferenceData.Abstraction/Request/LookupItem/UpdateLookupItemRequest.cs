namespace Aizen.Modules.ReferenceData.Abstraction.Request.LookupItem;

public sealed class UpdateLookupItemRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? IconKey { get; set; }
    public string? ColorCode { get; set; }
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}
