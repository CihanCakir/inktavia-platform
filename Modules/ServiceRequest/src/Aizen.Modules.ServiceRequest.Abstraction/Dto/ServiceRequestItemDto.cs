using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest item DTO", "Represents a single line item within a service request.")]
public sealed class ServiceRequestItemDto
{
    public long Id { get; set; }
    public ServiceRequestItemType ItemType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public string? UnitCode { get; set; }
    public decimal? EstimatedUnitPrice { get; set; }
    public int SortOrder { get; set; }
}
