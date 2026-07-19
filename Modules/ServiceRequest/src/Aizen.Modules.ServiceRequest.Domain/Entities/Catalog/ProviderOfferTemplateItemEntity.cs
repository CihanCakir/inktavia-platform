using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Catalog;

public sealed class ProviderOfferTemplateItemEntity : AizenEntityWithAudit
{
    public long ProviderOfferTemplateId { get; private set; }
    public ServiceRequestOfferItemType ItemType { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Quantity { get; private set; }
    public string? UnitCode { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string CurrencyCode { get; private set; } = "TRY";
    public decimal TaxRate { get; private set; }
    public OfferDiscountType DiscountType { get; private set; }
    public decimal? DiscountValue { get; private set; }
    public int SortOrder { get; private set; }

    public ProviderOfferTemplateEntity Template { get; private set; } = default!;

    public ProviderOfferTemplateItemEntity() { }

    public static ProviderOfferTemplateItemEntity Create(
        ServiceRequestOfferItemType itemType, string title, string? description,
        decimal quantity, string? unitCode, decimal unitPrice, string currencyCode,
        decimal taxRate, OfferDiscountType discountType, decimal? discountValue, int sortOrder)
    {
        return new ProviderOfferTemplateItemEntity
        {
            ItemType = itemType, Title = title.Trim(), Description = description,
            Quantity = quantity > 0 ? quantity : 1, UnitCode = unitCode,
            UnitPrice = unitPrice, CurrencyCode = currencyCode.ToUpperInvariant(),
            TaxRate = taxRate, DiscountType = discountType, DiscountValue = discountValue,
            SortOrder = sortOrder, IsActive = true
        };
    }
}
