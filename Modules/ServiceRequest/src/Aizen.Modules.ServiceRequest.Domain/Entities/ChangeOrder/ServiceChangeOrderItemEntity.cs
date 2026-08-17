using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.ChangeOrder;

/// <summary>
/// BE-S11b — a proposed line on a <see cref="ServiceChangeOrderEntity"/>. Same input shape as an offer item
/// (type/qty/unitPrice/currency/tax/pricingMethod/eligibility). <b>Immutable once proposed</b> — there are no mutators; a
/// change requires a new change order. The computed line economics are derived on apply by running the shared
/// <c>OfferCalculationService</c> over a transient offer built from these inputs (no economics math lives here).
/// </summary>
public sealed class ServiceChangeOrderItemEntity : AizenEntityWithAudit
{
    public long ServiceChangeOrderId { get; private set; }
    public ServiceRequestOfferItemType ItemType { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string CurrencyCode { get; private set; } = "TRY";
    public int SortOrder { get; private set; }
    public string? UnitCode { get; private set; }
    public decimal TaxRate { get; private set; }
    public OfferDiscountType DiscountType { get; private set; }
    public decimal? DiscountValue { get; private set; }
    public PricingMethod PricingMethod { get; private set; } = PricingMethod.Fixed;
    public LineCommissionEligibility CommissionEligibility { get; private set; } = LineCommissionEligibility.InheritFromCategory;
    public LineDiscountEligibility LineDiscountEligibility { get; private set; } = LineDiscountEligibility.InheritFromCategory;

    public ServiceChangeOrderEntity ChangeOrder { get; private set; } = default!;

    public ServiceChangeOrderItemEntity() { }

    public static ServiceChangeOrderItemEntity Create(
        ServiceRequestOfferItemType itemType,
        string title,
        string? description,
        decimal quantity,
        decimal unitPrice,
        string currencyCode,
        int sortOrder,
        string? unitCode = null,
        decimal taxRate = 0m,
        OfferDiscountType discountType = OfferDiscountType.None,
        decimal? discountValue = null,
        PricingMethod pricingMethod = PricingMethod.Fixed,
        LineCommissionEligibility? commissionEligibility = null,
        LineDiscountEligibility? discountEligibility = null)
    {
        return new ServiceChangeOrderItemEntity
        {
            ItemType                = itemType,
            Title                   = title.Trim(),
            Description             = description,
            Quantity                = quantity > 0 ? quantity : 1,
            UnitPrice               = unitPrice,
            CurrencyCode            = currencyCode.ToUpperInvariant(),
            SortOrder               = sortOrder,
            UnitCode                = unitCode,
            TaxRate                 = taxRate,
            DiscountType            = discountType,
            DiscountValue           = discountValue,
            PricingMethod           = pricingMethod,
            CommissionEligibility   = commissionEligibility ?? Offer.ServiceRequestOfferItemEntity.DefaultEligibilityForItemType(itemType),
            LineDiscountEligibility = discountEligibility ?? Offer.ServiceRequestOfferItemEntity.DefaultDiscountEligibilityForItemType(itemType),
            IsActive                = true,
        };
    }
}
