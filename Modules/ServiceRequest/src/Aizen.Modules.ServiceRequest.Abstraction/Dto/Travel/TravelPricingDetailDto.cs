using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto.Travel;

/// <summary>BE-S4a — the structured travel-pricing detail currently on a Travel offer line (provider read + set response).</summary>
public sealed class TravelPricingDetailDto
{
    public long               OfferItemId         { get; set; }
    public TravelPricingMethod Method             { get; set; }
    public string?            OriginCityCode      { get; set; }
    public string?            DestinationCityCode { get; set; }
    public decimal?           DistanceKm          { get; set; }
    public decimal?           PerKmRate           { get; set; }
    public string?            UnitCode            { get; set; }
}
