using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

public sealed class ProviderOfferTemplateDto
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public List<ProviderOfferTemplateItemDto> Items { get; set; } = new();
}

public sealed class ProviderOfferTemplateItemDto
{
    public long Id { get; set; }
    public ServiceRequestOfferItemType ItemType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public string? UnitCode { get; set; }
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public decimal TaxRate { get; set; }
    public OfferDiscountType DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public int SortOrder { get; set; }
}
