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
    /// <summary>
    /// S2d — the line's pricing attribute values (already resolved SR-side: item code + denormalized display label).
    /// Descriptive metadata — Payment snapshots them alongside the line economics and never uses them in the money math.
    /// </summary>
    public List<CalculateServiceRequestEconomicsAttributeDto> Attributes { get; init; } = new();

    /// <summary>
    /// S3 — the FX conversion applied to this line at offer-submit (source currency + price, the applied R1 rate, and the
    /// resolved TRY unit price). Null ⇒ a settlement-native line (no conversion). Descriptive/frozen metadata — the
    /// economics already runs on the converted TRY amounts; Payment persists this self-contained on the acceptance snapshot
    /// so the accepted total never re-values against a later rate change.
    /// </summary>
    public CalculateServiceRequestEconomicsLineFxDto? Fx { get; init; }

    /// <summary>
    /// S4 — the structured travel/mobilization derivation for a Travel line (method, origin/destination city codes + denormalized
    /// labels, provider-declared distance, per-km rate, KILOMETER unit). Null for a non-Travel line or a Travel line with no
    /// detail. Descriptive/frozen — Payment snapshots it beside the line economics (the Travel line amount is already an Exempt
    /// pass-through in the 8-equality); it enters no sum or invariant. The resolved travel amount is the line total (derived by
    /// Payment) and tamper-checked against <c>Round(DistanceKm × PerKmRate)</c> for PerKm.
    /// </summary>
    public CalculateServiceRequestEconomicsTravelDto? Travel { get; init; }
}

/// <summary>S4 — the structured travel/mobilization derivation threaded SR→Payment (self-contained; no re-lookup after acceptance).</summary>
public sealed class CalculateServiceRequestEconomicsTravelDto
{
    public required int      Method               { get; init; }   // raw SR TravelPricingMethod (1=FlatMobilization, 2=PerKm)
    public string?           OriginCityCode       { get; init; }
    public string?           OriginCityLabel      { get; init; }   // denormalized display label (no re-lookup after acceptance)
    public string?           DestinationCityCode  { get; init; }
    public string?           DestinationCityLabel { get; init; }   // denormalized display label
    public decimal?          DistanceKm           { get; init; }
    public decimal?          PerKmRate            { get; init; }
    public string?           UnitCode             { get; init; }   // KILOMETER for PerKm
}

/// <summary>S3 — the frozen per-line FX record threaded SR→Payment (self-contained; no re-resolve after acceptance).</summary>
public sealed class CalculateServiceRequestEconomicsLineFxDto
{
    public required string   SourceCurrencyCode     { get; init; }
    public required string   SettlementCurrencyCode { get; init; }
    public required decimal  SourceUnitPrice        { get; init; }
    public required decimal  AppliedRate            { get; init; }
    public required DateTime RateDate               { get; init; }
    /// <summary>The converted settlement-currency (TRY) unit price = round(SourceUnitPrice × AppliedRate).</summary>
    public required decimal  ResolvedUnitPrice      { get; init; }
}

/// <summary>S2d — one pricing attribute value to snapshot on a line at acceptance (§20.6).</summary>
public sealed class CalculateServiceRequestEconomicsAttributeDto
{
    public required string   DefinitionCode       { get; init; }
    public required int      DataType             { get; init; }   // raw SR PricingAttributeDataType
    public string?           ValueLookupItemCode  { get; init; }
    public string?           ValueLookupItemLabel { get; init; }   // denormalized display label (no re-lookup after acceptance)
    public decimal?          ValueNumber          { get; init; }
    public string?           ValueText            { get; init; }
    public bool?             ValueBool            { get; init; }
    public int               SortOrder            { get; init; }
}
