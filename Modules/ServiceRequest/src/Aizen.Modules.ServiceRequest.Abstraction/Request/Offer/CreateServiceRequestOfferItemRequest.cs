using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

[DocumentationInfo("Create service request offer item request", "Input model for a single line item within a provider offer.")]
public sealed class CreateServiceRequestOfferItemRequest
{
    public ServiceRequestOfferItemType ItemType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int SortOrder { get; set; }
}
