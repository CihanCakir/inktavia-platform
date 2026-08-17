using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Travel;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Services.Travel;

/// <summary>
/// BE-S4a — validates the structured travel-pricing detail a provider set on a <c>Travel</c> offer line. Resolves the external
/// facts (the KILOMETER unit via the R3 by-code remote call; the origin/destination city codes via GetCity) fail-loud, then
/// delegates to the pure <see cref="TravelPricingValidation"/> core. <b>Descriptive</b> — it never re-prices the line; it refuses
/// a detail that disagrees with the line's own money (Quantity/UnitPrice/PricingMethod) or names a non-KILOMETER unit.
/// </summary>
public sealed class TravelPricingValidator
{
    /// <summary>The R3-seeded distance unit a PerKm travel line must use.</summary>
    public const string KilometerUnitCode = "KILOMETER";

    private const string DefaultCountryCode = "TR";

    private readonly IServiceRequestReferenceDataRemoteCall _referenceData;

    public TravelPricingValidator(IServiceRequestReferenceDataRemoteCall referenceData)
        => _referenceData = referenceData;

    /// <summary>Validate <paramref name="request"/> against <paramref name="line"/>. Throws <c>SR_TRAVEL_*</c> on the first violation.</summary>
    public async Task ValidateAsync(ServiceRequestOfferItemEntity line, SetOfferLineTravelPricingRequest request, CancellationToken ct)
    {
        // Resolve the KILOMETER unit only for PerKm (Flat carries no unit). Null/inactive/non-KILOMETER ⇒ not resolved.
        var kilometerResolved = false;
        if (request.Method == TravelPricingMethod.PerKm && !string.IsNullOrWhiteSpace(request.UnitCode))
        {
            var unit = await _referenceData.GetMeasurementUnitByCode(request.UnitCode!.Trim());
            var body = unit.Body;
            kilometerResolved = body is { IsActive: true }
                && string.Equals(body.Code?.Trim(), KilometerUnitCode, StringComparison.OrdinalIgnoreCase);
        }

        // City codes are optional context — validate existence/active when supplied (fail-loud).
        await ValidateCityAsync(request.OriginCityCode, "SR_TRAVEL_UNKNOWN_ORIGIN_CITY", ct);
        await ValidateCityAsync(request.DestinationCityCode, "SR_TRAVEL_UNKNOWN_DESTINATION_CITY", ct);

        TravelPricingValidation.Validate(
            line.ItemType, line.Quantity, line.UnitPrice, line.PricingMethod,
            request.Method, request.DistanceKm, request.PerKmRate, request.UnitCode, kilometerResolved);
    }

    private async Task ValidateCityAsync(string? cityCode, string errorCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cityCode)) return;
        var result = await _referenceData.GetCity(DefaultCountryCode, cityCode.Trim());
        if (result.Body is null || !result.Body.IsActive)
            throw new AizenBusinessException($"{errorCode}: '{cityCode}'");
    }
}

/// <summary>
/// BE-S4a — the pure validation core (no I/O). Given the line's own money and the resolved KILOMETER-unit fact, it fails loud
/// (<c>SR_TRAVEL_*</c>) on: a travel detail on a non-Travel line; a PerKm detail with a non-positive distance/rate, a
/// non-KILOMETER unit, or a distance/rate/method that disagrees with the line; a FlatMobilization detail that carries a km/rate
/// or whose line is not a single fixed fee. It never re-prices the line.
/// </summary>
public static class TravelPricingValidation
{
    public static void Validate(
        ServiceRequestOfferItemType itemType,
        decimal lineQuantity, decimal lineUnitPrice, PricingMethod linePricingMethod,
        TravelPricingMethod method, decimal? distanceKm, decimal? perKmRate, string? unitCode,
        bool kilometerUnitResolved)
    {
        // Only on a Travel line (§20.8).
        if (itemType != ServiceRequestOfferItemType.Travel)
            throw new AizenBusinessException($"SR_TRAVEL_NOT_TRAVEL_LINE: travel pricing is only allowed on a Travel line (got {itemType}).");

        switch (method)
        {
            case TravelPricingMethod.PerKm:
                if (!distanceKm.HasValue || distanceKm.Value <= 0m)
                    throw new AizenBusinessException("SR_TRAVEL_DISTANCE_REQUIRED: PerKm requires distanceKm > 0.");
                if (!perKmRate.HasValue || perKmRate.Value <= 0m)
                    throw new AizenBusinessException("SR_TRAVEL_RATE_REQUIRED: PerKm requires perKmRate > 0.");
                if (string.IsNullOrWhiteSpace(unitCode) || !kilometerUnitResolved)
                    throw new AizenBusinessException("SR_TRAVEL_UNIT_NOT_KILOMETER: PerKm unitCode must resolve to the active KILOMETER unit.");
                // Consistency with the line money (so the snapshot can never disagree with the money).
                if (lineQuantity != distanceKm.Value)
                    throw new AizenBusinessException($"SR_TRAVEL_QUANTITY_MISMATCH: line Quantity {lineQuantity} != distanceKm {distanceKm.Value}.");
                if (lineUnitPrice != perKmRate.Value)
                    throw new AizenBusinessException($"SR_TRAVEL_RATE_MISMATCH: line UnitPrice {lineUnitPrice} != perKmRate {perKmRate.Value}.");
                if (linePricingMethod != PricingMethod.PerKm)
                    throw new AizenBusinessException($"SR_TRAVEL_METHOD_MISMATCH: PerKm travel requires line PricingMethod PerKm (got {linePricingMethod}).");
                break;

            case TravelPricingMethod.FlatMobilization:
                if (distanceKm.HasValue)
                    throw new AizenBusinessException("SR_TRAVEL_FLAT_NO_KM: FlatMobilization must not carry a distanceKm.");
                if (perKmRate.HasValue)
                    throw new AizenBusinessException("SR_TRAVEL_FLAT_NO_RATE: FlatMobilization must not carry a perKmRate.");
                if (!string.IsNullOrWhiteSpace(unitCode))
                    throw new AizenBusinessException("SR_TRAVEL_FLAT_NO_UNIT: FlatMobilization must not carry a unitCode.");
                if (lineQuantity != 1m)
                    throw new AizenBusinessException($"SR_TRAVEL_FLAT_QUANTITY: FlatMobilization requires line Quantity 1 (got {lineQuantity}).");
                if (lineUnitPrice <= 0m)
                    throw new AizenBusinessException($"SR_TRAVEL_FLAT_FEE_REQUIRED: FlatMobilization requires a positive line UnitPrice (fee) (got {lineUnitPrice}).");
                if (linePricingMethod != PricingMethod.Fixed)
                    throw new AizenBusinessException($"SR_TRAVEL_METHOD_MISMATCH: FlatMobilization travel requires line PricingMethod Fixed (got {linePricingMethod}).");
                break;

            default:
                throw new AizenBusinessException($"SR_TRAVEL_UNKNOWN_METHOD: '{method}'.");
        }
    }
}
