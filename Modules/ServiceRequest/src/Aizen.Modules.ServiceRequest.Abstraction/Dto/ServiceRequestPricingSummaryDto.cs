
namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest pricing summary DTO", "Aggregated pricing summary for an accepted offer.")]
public sealed class ServiceRequestPricingSummaryDto
{
    public long OfferId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public List<ServiceRequestOfferItemDto> LineItems { get; set; } = new();
}
