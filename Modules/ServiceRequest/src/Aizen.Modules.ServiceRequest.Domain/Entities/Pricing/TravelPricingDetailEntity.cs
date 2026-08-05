using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Pricing;

/// <summary>
/// BE-S4a (§20.8) — the structured <b>travel / mobilization</b> pricing detail a provider set on one <c>Travel</c> offer line
/// (line-level, consistent with S1/S2/S8). Bound to the offer item by <see cref="OfferItemId"/> (at most one per line). Records
/// <i>how</i> the travel charge was derived — a flat mobilization fee or a per-km × distance figure — plus the origin/destination
/// cities (display/context) and the KILOMETER unit for PerKm. <b>Descriptive</b>: the Travel line's money is priced by the standard
/// line math (Exempt pass-through in the 8-equality); this never re-prices the line. Mutable pre-acceptance (the provider edits
/// their offer); at acceptance it is copied into the immutable <c>TravelPricingSnapshot</c> (S4b). Distance is provider-declared
/// (geo compute deferred to GeoDiscovery).
/// </summary>
public sealed class TravelPricingDetailEntity : AizenEntityWithAudit
{
    public long               OfferItemId         { get; private set; }
    public TravelPricingMethod Method             { get; private set; }
    public string?            OriginCityCode      { get; private set; }
    public string?            DestinationCityCode { get; private set; }
    public decimal?           DistanceKm          { get; private set; }
    public decimal?           PerKmRate           { get; private set; }
    public string?            UnitCode            { get; private set; }

    private TravelPricingDetailEntity() { }

    public static TravelPricingDetailEntity Create(
        long offerItemId, TravelPricingMethod method,
        string? originCityCode, string? destinationCityCode,
        decimal? distanceKm, decimal? perKmRate, string? unitCode)
        => new()
        {
            OfferItemId         = offerItemId,
            Method              = method,
            OriginCityCode      = Normalize(originCityCode),
            DestinationCityCode = Normalize(destinationCityCode),
            DistanceKm          = distanceKm,
            PerKmRate           = perKmRate,
            UnitCode            = Normalize(unitCode),
            IsActive            = true,
        };

    public void SetValue(
        TravelPricingMethod method, string? originCityCode, string? destinationCityCode,
        decimal? distanceKm, decimal? perKmRate, string? unitCode)
    {
        Method              = method;
        OriginCityCode      = Normalize(originCityCode);
        DestinationCityCode = Normalize(destinationCityCode);
        DistanceKm          = distanceKm;
        PerKmRate           = perKmRate;
        UnitCode            = Normalize(unitCode);
    }

    private static string? Normalize(string? code)
        => string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
}
