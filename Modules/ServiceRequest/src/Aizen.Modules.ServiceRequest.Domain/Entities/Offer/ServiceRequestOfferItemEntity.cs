using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Offer;

[DocumentationInfo("ServiceRequest offer item entity", "Line item within a service offer with type, quantity, and pricing.")]
public sealed class ServiceRequestOfferItemEntity : AizenEntityWithAudit
{
    public long ServiceRequestOfferId { get; private set; }
    public ServiceRequestOfferItemType ItemType { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public int SortOrder { get; private set; }
    public string? UnitCode { get; private set; }
    public decimal TaxRate { get; private set; }

    // Discount inputs
    public OfferDiscountType DiscountType { get; private set; }
    public decimal? DiscountValue { get; private set; }

    // Computed snapshots — set by the calculation service (10c), not by domain methods
    public decimal LineSubtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }

    public ServiceRequestOfferEntity Offer { get; private set; } = default!;

    public ServiceRequestOfferItemEntity() { }

    public static ServiceRequestOfferItemEntity Create(
        long serviceRequestOfferId,
        ServiceRequestOfferItemType itemType,
        string title,
        string? description,
        decimal quantity,
        decimal unitPrice,
        string currencyCode,
        int sortOrder,
        string? unitCode = null,
        decimal taxRate = 0,
        OfferDiscountType discountType = OfferDiscountType.None,
        decimal? discountValue = null)
    {
        return new ServiceRequestOfferItemEntity
        {
            ServiceRequestOfferId = serviceRequestOfferId,
            ItemType = itemType,
            Title = title.Trim(),
            Description = description,
            Quantity = quantity > 0 ? quantity : 1,
            UnitPrice = unitPrice,
            CurrencyCode = currencyCode.ToUpperInvariant(),
            SortOrder = sortOrder,
            UnitCode = unitCode,
            TaxRate = taxRate,
            DiscountType = discountType,
            DiscountValue = discountValue,
            IsActive = true
        };
    }

    /// <summary>
    /// Sets the computed line totals. Called by the calculation service only.
    /// </summary>
    public void SetComputedTotals(decimal lineSubtotal, decimal discountAmount, decimal taxAmount, decimal lineTotal)
    {
        LineSubtotal = lineSubtotal;
        DiscountAmount = discountAmount;
        TaxAmount = taxAmount;
        LineTotal = lineTotal;
    }
}
