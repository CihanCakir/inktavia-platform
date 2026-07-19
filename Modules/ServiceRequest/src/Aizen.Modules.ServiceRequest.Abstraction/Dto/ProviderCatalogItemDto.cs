using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

public sealed class ProviderCatalogItemDto
{
    public long Id { get; set; }
    public ServiceRequestOfferItemType ItemType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal DefaultQuantity { get; set; }
    public string? UnitCode { get; set; }
    public decimal DefaultUnitPrice { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public decimal DefaultTaxRate { get; set; }
}
