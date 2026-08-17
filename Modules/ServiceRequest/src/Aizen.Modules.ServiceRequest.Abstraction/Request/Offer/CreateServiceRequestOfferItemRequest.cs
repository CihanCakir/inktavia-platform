using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

[DocumentationInfo("Create service request offer item request", "Input model for a single line item within a provider offer.")]
public sealed class CreateServiceRequestOfferItemRequest
{
    public ServiceRequestOfferItemType ItemType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int SortOrder { get; set; }
    public string? UnitCode { get; set; }
    public decimal TaxRate { get; set; }
    public OfferDiscountType DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }

    // ── Line economics inputs (BE-S1) ─────────────────────────────────────────
    /// <summary>How the price was derived (§20.4). Descriptive; defaults to Fixed. Money math is unchanged.</summary>
    public PricingMethod PricingMethod { get; set; } = PricingMethod.Fixed;
    /// <summary>Explicit per-line commission eligibility (§20.11). Null = use the item-type default map.</summary>
    public LineCommissionEligibility? CommissionEligibility { get; set; }
}
