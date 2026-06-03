using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest offer item DTO", "Represents a single line item within a provider offer.")]
public sealed class ServiceRequestOfferItemDto
{
    public long Id { get; set; }
    public ServiceRequestOfferItemType ItemType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int SortOrder { get; set; }
    public bool IsDiscount { get; set; }
}
