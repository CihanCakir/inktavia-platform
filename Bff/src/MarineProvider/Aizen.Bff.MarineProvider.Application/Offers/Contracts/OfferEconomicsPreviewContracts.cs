namespace Aizen.Bff.MarineProvider.Application.Offers.Contracts;

/// <summary>
/// BE-S7 commission-preview request body. <c>ProviderProfileId</c> is deliberately NOT accepted from the client —
/// the BFF resolves it by-subject. Lines carry RAW pricing inputs; the BFF computes CommissionBaseAmount /
/// CommissionEligibility / LineProviderRevenue (mirroring S1 OfferCalculationService) before proxying to Payment.
/// </summary>
public sealed class OfferCommissionPreviewBffRequest
{
    public long?                            ProviderPlanId { get; init; }
    public string                           CurrencyCode   { get; init; } = "TRY";
    /// <summary>Provider offer-level discount (flat amount, applied pro-rata across lines).</summary>
    public decimal                          OfferDiscountAmount { get; init; }
    public List<OfferCommissionLineInputBff> Lines         { get; init; } = new();
}

/// <summary>
/// A single priced offer line — RAW inputs as the FE sends them. <c>ItemType</c> is the numeric
/// <c>ServiceRequestOfferItemType</c> enum value (Service=1, Product=2, …) — canonical wire form.
/// The BFF computes CommissionBaseAmount / CommissionEligibility / LineProviderRevenue server-side.
/// </summary>
public sealed class OfferCommissionLineInputBff
{
    /// <summary>Offer-item id / correlation key echoed back on the result.</summary>
    public string  LineRef     { get; init; } = default!;
    /// <summary>Numeric ServiceRequestOfferItemType (Service=1, Product=2, Installation=3, Delivery=4,
    /// Labor=5, Inspection=6, EmergencyFee=7, Discount=8, Consumable=9, Travel=10, ExternalService=11,
    /// EquipmentRental=12, MarinaOrLiftFee=13, OtherApprovedExpense=14, Other=99).</summary>
    public int     ItemType    { get; init; }
    public decimal Quantity    { get; init; }
    public decimal UnitPrice   { get; init; }
    public decimal TaxRate     { get; init; }
    public string? ProductCode  { get; init; }
    public string? CategoryCode { get; init; }
    /// <summary>Optional explicit override (string: "Eligible"/"Exempt"/"InheritFromCategory"). If omitted, derived from ItemType.</summary>
    public string? CommissionEligibility { get; init; }
}

/// <summary>
/// BE-S6 customer-discount-preview request body. Carries the offer's raw lines so the BFF can compute
/// EligibleServiceBaseAmount server-side. The FE does NOT need to pre-compute it.
/// </summary>
public sealed class OfferCustomerDiscountPreviewBffRequest
{
    public long?   CustomerPlanId { get; init; }
    public string? CategoryCode   { get; init; }
    public string  CurrencyCode   { get; init; } = "TRY";
    /// <summary>Provider offer-level discount (flat amount, applied pro-rata across eligible lines).</summary>
    public decimal OfferDiscountAmount { get; init; }
    /// <summary>Raw line inputs — BFF computes the eligible pre-tax base from these.</summary>
    public List<OfferDiscountLineInputBff> Lines { get; init; } = new();
}

/// <summary>Raw line input for customer-discount-preview.</summary>
public sealed class OfferDiscountLineInputBff
{
    /// <summary>Numeric ServiceRequestOfferItemType.</summary>
    public int     ItemType    { get; init; }
    public decimal Quantity    { get; init; }
    public decimal UnitPrice   { get; init; }
    public decimal TaxRate     { get; init; }
}
