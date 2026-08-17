namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

/// <summary>
/// BE-S4 (§20.8) — how a provider derived the <b>travel / mobilization</b> charge on a <see cref="ServiceRequestOfferItemType.Travel"/>
/// line. Descriptive only: the Travel line's money is priced by the standard line math (it is an Exempt pass-through in the
/// 8-equality); this records <i>how</i> the amount was arrived at and lets S4 validate the derivation against the line.
/// </summary>
public enum TravelPricingMethod
{
    /// <summary>A single fixed mobilization fee (line <c>Quantity = 1</c>, <c>UnitPrice = fee</c>, <see cref="PricingMethod.Fixed"/>).</summary>
    FlatMobilization = 1,

    /// <summary>Distance × per-km rate (line <c>Quantity = distanceKm</c>, <c>UnitPrice = perKmRate</c>, <see cref="PricingMethod.PerKm"/>, unit KILOMETER).</summary>
    PerKm            = 2,
}
