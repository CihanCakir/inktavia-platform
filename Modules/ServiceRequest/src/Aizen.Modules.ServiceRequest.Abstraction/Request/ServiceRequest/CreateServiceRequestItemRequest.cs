using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;

[DocumentationInfo("Create service request item request", "Input model for a single item within a new service request.")]
public sealed class CreateServiceRequestItemRequest
{
    public ServiceRequestItemType ItemType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public int Quantity { get; set; } = 1;
    public string? UnitCode { get; set; }
    public decimal? EstimatedUnitPrice { get; set; }
    public int SortOrder { get; set; }
}
