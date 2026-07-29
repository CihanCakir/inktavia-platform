using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;

/// <summary>
/// BE-P8 — the ServiceRequest → Payment acceptance-economics request. Carries the offer line set + provider/customer/
/// category/currency; Payment runs the §19.9 combiner (plan → S7 commission → P3 fee → P5 gate → S8 snapshot) and, on an
/// approving decision, creates the escrow with CustomerTotal gross / ProviderNet split and links the snapshot. Idempotent
/// on <see cref="IdempotencyKey"/> = <c>SR-{ServiceRequestId}-OFFER-{OfferId}</c>.
/// </summary>
public sealed class CalculateServiceRequestEconomicsRemoteCallRequest
{
    public required string IdempotencyKey    { get; init; }   // SR-{srId}-OFFER-{offerId}
    public required long   ServiceRequestId  { get; init; }
    public required long   OfferId           { get; init; }
    public required long   ProviderProfileId { get; init; }
    public required long   CustomerProfileId { get; init; }
    public long?           CustomerPlanId    { get; init; }
    /// <summary>BE-P8b — participant plan subscription for the customer benefit budget (null → no per-customer budget cap).</summary>
    public long?           ParticipantPlanSubscriptionId { get; init; }
    public string?         CategoryCode      { get; init; }
    public required string CurrencyCode      { get; init; }
    public required List<CalculateServiceRequestEconomicsLineDto> Lines { get; init; } = new();
}

/// <summary>
/// One priced offer line. Discount items are excluded by the caller. <see cref="LineGrossBeforeDiscount"/> is the
/// post-provider-discount, pre-tax provider revenue; Payment derives <c>LineTotal = LineGrossBeforeDiscount + LineVat</c>.
/// </summary>
public sealed class CalculateServiceRequestEconomicsLineDto
{
    public required string                    LineRef                 { get; init; }
    public required int                       ItemType                { get; init; }   // raw SR ServiceRequestOfferItemType
    public required int                       PricingMethod           { get; init; }   // raw SR PricingMethod
    public LineType?                          CommissionLineType      { get; init; }   // commission-rule dimension
    public required LineCommissionEligibility CommissionEligibility   { get; init; }
    public string?                            ProductCode             { get; init; }
    public required decimal                   LineGrossBeforeDiscount { get; init; }
    public required decimal                   LineVat                 { get; init; }
    public required decimal                   CommissionBaseAmount    { get; init; }
    public required decimal                   LineProviderRevenue     { get; init; }
    /// <summary>BE-S6/P8b — eligible for a customer (platform/plan) discount. Default true; SR sets it from LineDiscountEligibility.</summary>
    public bool                               DiscountEligible        { get; init; } = true;
}
