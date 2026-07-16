using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest offer item DTO", "Represents a single line item within a provider offer.")]
public sealed class ServiceRequestOfferItemDto
{
    public long Id { get; set; }
    public ServiceRequestOfferItemType ItemType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int SortOrder { get; set; }
    public string? UnitCode { get; set; }
    public decimal TaxRate { get; set; }
    public OfferDiscountType DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal DiscountAmount { get; set; }
}
