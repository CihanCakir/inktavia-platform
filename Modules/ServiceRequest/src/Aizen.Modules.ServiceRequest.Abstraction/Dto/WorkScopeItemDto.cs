using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

/// <summary>
/// Work-scope item visible to providers. No EstimatedUnitPrice (owner's price estimate is hidden).
/// </summary>
public sealed class WorkScopeItemDto
{
    public long Id { get; set; }
    public ServiceRequestItemType ItemType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public string? UnitCode { get; set; }
    public int SortOrder { get; set; }
}
