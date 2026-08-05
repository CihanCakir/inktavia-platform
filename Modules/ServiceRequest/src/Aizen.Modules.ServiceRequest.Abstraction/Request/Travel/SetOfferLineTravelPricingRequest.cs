using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Travel;

/// <summary>
/// BE-S4a — provider payload to set (upsert) the structured travel-pricing detail on one <c>Travel</c> offer line. Descriptive:
/// it records how the travel charge was derived and is validated against the line's own money (Quantity/UnitPrice/PricingMethod);
/// it does <b>not</b> re-price the line. Distance is provider-declared (geo compute is deferred to GeoDiscovery).
/// </summary>
public sealed class SetOfferLineTravelPricingRequest
{
    public TravelPricingMethod Method { get; set; }

    /// <summary>Provider's origin city (ReferenceData city code, validated via GetCity). Display/context only.</summary>
    public string? OriginCityCode { get; set; }

    /// <summary>The vessel's (destination) city (ReferenceData city code, validated via GetCity). Display/context only.</summary>
    public string? DestinationCityCode { get; set; }

    /// <summary>Provider-declared distance in km (PerKm only). Must equal the line's Quantity.</summary>
    public decimal? DistanceKm { get; set; }

    /// <summary>Per-km rate in the line currency (PerKm only). Must equal the line's UnitPrice.</summary>
    public decimal? PerKmRate { get; set; }

    /// <summary>Measurement unit (PerKm only) — must resolve to the R3 KILOMETER unit.</summary>
    public string? UnitCode { get; set; }
}
