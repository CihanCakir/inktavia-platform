using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Catalog;

public sealed class ProviderCatalogItemEntity : AizenEntityWithAudit
{
    public long ProviderProfileId { get; private set; }
    public ServiceRequestOfferItemType ItemType { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal DefaultQuantity { get; private set; }
    public string? UnitCode { get; private set; }
    public decimal DefaultUnitPrice { get; private set; }
    public string CurrencyCode { get; private set; } = "TRY";
    public decimal DefaultTaxRate { get; private set; }

    public ProviderCatalogItemEntity() { }

    public static ProviderCatalogItemEntity Create(
        long providerProfileId,
        ServiceRequestOfferItemType itemType,
        string title,
        string? description,
        decimal defaultQuantity,
        string? unitCode,
        decimal defaultUnitPrice,
        string currencyCode,
        decimal defaultTaxRate)
    {
        return new ProviderCatalogItemEntity
        {
            ProviderProfileId = providerProfileId,
            ItemType = itemType,
            Title = title.Trim(),
            Description = description,
            DefaultQuantity = defaultQuantity > 0 ? defaultQuantity : 1,
            UnitCode = unitCode,
            DefaultUnitPrice = defaultUnitPrice,
            CurrencyCode = currencyCode.ToUpperInvariant(),
            DefaultTaxRate = defaultTaxRate,
            IsActive = true
        };
    }

    public void Update(
        ServiceRequestOfferItemType itemType,
        string title,
        string? description,
        decimal defaultQuantity,
        string? unitCode,
        decimal defaultUnitPrice,
        string currencyCode,
        decimal defaultTaxRate)
    {
        ItemType = itemType;
        Title = title.Trim();
        Description = description;
        DefaultQuantity = defaultQuantity > 0 ? defaultQuantity : 1;
        UnitCode = unitCode;
        DefaultUnitPrice = defaultUnitPrice;
        CurrencyCode = currencyCode.ToUpperInvariant();
        DefaultTaxRate = defaultTaxRate;
    }

    public void Deactivate() => IsActive = false;
}
