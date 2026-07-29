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

    // ── Line economics inputs (BE-S1) ─────────────────────────────────────────
    /// <summary>How the price was derived (§20.4). Descriptive only — the money math is unchanged.</summary>
    public PricingMethod PricingMethod { get; private set; } = PricingMethod.Fixed;
    /// <summary>Per-line commission eligibility dimension (§20.11). Rate resolution is S7.</summary>
    public LineCommissionEligibility CommissionEligibility { get; private set; } = LineCommissionEligibility.InheritFromCategory;

    /// <summary>BE-S6 — per-line eligibility for a customer (platform/plan) discount (§20.10). Exempt → no customer discount.</summary>
    public LineDiscountEligibility LineDiscountEligibility { get; private set; } = LineDiscountEligibility.InheritFromCategory;

    // Computed snapshots — set by the calculation service (10c), not by domain methods
    public decimal LineSubtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }

    /// <summary>
    /// Computed (§20.5): the pre-tax, post-line-discount revenue share commission WILL apply to in S7. Eligible /
    /// InheritFromCategory → Max(LineSubtotal − DiscountAmount − CustomerDiscountAmount, 0); Exempt → 0. No rate here.
    /// </summary>
    public decimal CommissionBaseAmount { get; private set; }

    // ── Customer discount funding (BE-S6) — computed; 0 unless a customer discount is applied ──
    public decimal CustomerDiscountAmount       { get; private set; }
    public decimal PlatformFundedDiscountAmount { get; private set; }
    public decimal ProviderFundedDiscountAmount { get; private set; }

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
        decimal? discountValue = null,
        PricingMethod pricingMethod = PricingMethod.Fixed,
        LineCommissionEligibility? commissionEligibility = null,
        LineDiscountEligibility? discountEligibility = null)
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
            PricingMethod = pricingMethod,
            // Explicit override when supplied; otherwise the item-type default (admin-tunable, not baked).
            CommissionEligibility  = commissionEligibility ?? DefaultEligibilityForItemType(itemType),
            LineDiscountEligibility = discountEligibility ?? DefaultDiscountEligibilityForItemType(itemType),
            IsActive = true
        };
    }

    /// <summary>
    /// BE-S6 default customer-discount eligibility by economic role (§20.10): service work + goods → Eligible;
    /// pass-through expenses (Travel/marina/rental/external) → Exempt; misc → InheritFromCategory. Starting default only;
    /// admin/category/per-line override may change it.
    /// </summary>
    public static LineDiscountEligibility DefaultDiscountEligibilityForItemType(ServiceRequestOfferItemType itemType)
        => itemType switch
        {
            ServiceRequestOfferItemType.Service              => LineDiscountEligibility.Eligible,
            ServiceRequestOfferItemType.Labor                => LineDiscountEligibility.Eligible,
            ServiceRequestOfferItemType.Installation         => LineDiscountEligibility.Eligible,
            ServiceRequestOfferItemType.Inspection           => LineDiscountEligibility.Eligible,
            ServiceRequestOfferItemType.EmergencyFee         => LineDiscountEligibility.Eligible,
            ServiceRequestOfferItemType.Delivery             => LineDiscountEligibility.Eligible,
            ServiceRequestOfferItemType.Product              => LineDiscountEligibility.Eligible,
            ServiceRequestOfferItemType.Consumable           => LineDiscountEligibility.Eligible,

            ServiceRequestOfferItemType.Travel               => LineDiscountEligibility.Exempt,
            ServiceRequestOfferItemType.ExternalService      => LineDiscountEligibility.Exempt,
            ServiceRequestOfferItemType.EquipmentRental      => LineDiscountEligibility.Exempt,
            ServiceRequestOfferItemType.MarinaOrLiftFee      => LineDiscountEligibility.Exempt,
            ServiceRequestOfferItemType.OtherApprovedExpense => LineDiscountEligibility.Exempt,

            ServiceRequestOfferItemType.Other                => LineDiscountEligibility.InheritFromCategory,
            _                                                => LineDiscountEligibility.Exempt,   // Discount + unknown
        };

    /// <summary>
    /// Sane default commission eligibility by the item's economic role (§20.3/§20.11). Revenue-bearing service work is
    /// Eligible; pass-through expenses are Exempt; goods defer to the category (InheritFromCategory). This is a starting
    /// default only — S7/admin/category configuration may override it per line; it is NOT a hardcoded business constant.
    /// </summary>
    public static LineCommissionEligibility DefaultEligibilityForItemType(ServiceRequestOfferItemType itemType)
        => itemType switch
        {
            ServiceRequestOfferItemType.Service              => LineCommissionEligibility.Eligible,
            ServiceRequestOfferItemType.Labor                => LineCommissionEligibility.Eligible,
            ServiceRequestOfferItemType.Installation         => LineCommissionEligibility.Eligible,
            ServiceRequestOfferItemType.Inspection           => LineCommissionEligibility.Eligible,
            ServiceRequestOfferItemType.EmergencyFee         => LineCommissionEligibility.Eligible,
            ServiceRequestOfferItemType.Delivery             => LineCommissionEligibility.Eligible,

            ServiceRequestOfferItemType.Product              => LineCommissionEligibility.InheritFromCategory,
            ServiceRequestOfferItemType.Consumable           => LineCommissionEligibility.InheritFromCategory,
            ServiceRequestOfferItemType.Other                => LineCommissionEligibility.InheritFromCategory,

            ServiceRequestOfferItemType.Travel               => LineCommissionEligibility.Exempt,
            ServiceRequestOfferItemType.ExternalService      => LineCommissionEligibility.Exempt,
            ServiceRequestOfferItemType.EquipmentRental      => LineCommissionEligibility.Exempt,
            ServiceRequestOfferItemType.MarinaOrLiftFee      => LineCommissionEligibility.Exempt,
            ServiceRequestOfferItemType.OtherApprovedExpense => LineCommissionEligibility.Exempt,

            _                                                => LineCommissionEligibility.Exempt,   // Discount + unknown
        };

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

    /// <summary>Sets the computed line economics (BE-S1). Called by the calculation service only, after the tax pass.</summary>
    public void SetComputedEconomics(decimal commissionBaseAmount)
    {
        CommissionBaseAmount = commissionBaseAmount;
    }

    /// <summary>Allows an explicit per-line eligibility override (e.g. admin edit) before recalculation.</summary>
    public void SetCommissionEligibility(LineCommissionEligibility eligibility)
    {
        CommissionEligibility = eligibility;
    }

    /// <summary>BE-S6 — sets the computed per-line customer discount + funding split. Called by the calculation service only.</summary>
    public void SetCustomerDiscount(decimal customerDiscount, decimal platformFunded, decimal providerFunded)
    {
        CustomerDiscountAmount       = customerDiscount;
        PlatformFundedDiscountAmount = platformFunded;
        ProviderFundedDiscountAmount = providerFunded;
    }

    /// <summary>Allows an explicit per-line customer-discount eligibility override before recalculation.</summary>
    public void SetLineDiscountEligibility(LineDiscountEligibility eligibility)
    {
        LineDiscountEligibility = eligibility;
    }
}
