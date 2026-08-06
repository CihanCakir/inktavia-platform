using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.ChangeOrder;

/// <summary>BE-S11b — provider proposes a post-acceptance change order (extra or removed work) on an accepted SR.</summary>
public sealed class ProposeServiceChangeOrderRequest
{
    public long AcceptedOfferId { get; set; }
    public ServiceChangeOrderDirection Direction { get; set; } = ServiceChangeOrderDirection.Increase;
    public string Reason { get; set; } = default!;
    public List<ServiceChangeOrderItemInput> Items { get; set; } = new();
}

/// <summary>BE-S11b — a proposed change-order line (same input shape as an offer item).</summary>
public sealed class ServiceChangeOrderItemInput
{
    public ServiceRequestOfferItemType ItemType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public int? SortOrder { get; set; }
    public string? UnitCode { get; set; }
    public decimal TaxRate { get; set; }
    public OfferDiscountType DiscountType { get; set; } = OfferDiscountType.None;
    public decimal? DiscountValue { get; set; }
    public PricingMethod PricingMethod { get; set; } = PricingMethod.Fixed;
    public LineCommissionEligibility? CommissionEligibility { get; set; }
    public LineDiscountEligibility? DiscountEligibility { get; set; }
}

/// <summary>BE-S11b — owner rejects a proposed change order (terminal).</summary>
public sealed class RejectServiceChangeOrderRequest
{
    public string? Reason { get; set; }
}
