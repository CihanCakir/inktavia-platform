using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

public sealed class OfferTemplateRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public List<OfferTemplateItemRequest> Items { get; set; } = new();
}

public sealed class OfferTemplateItemRequest
{
    public ServiceRequestOfferItemType ItemType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Quantity { get; set; } = 1;
    public string? UnitCode { get; set; }
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public decimal TaxRate { get; set; }
    public OfferDiscountType DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public int SortOrder { get; set; }
}
