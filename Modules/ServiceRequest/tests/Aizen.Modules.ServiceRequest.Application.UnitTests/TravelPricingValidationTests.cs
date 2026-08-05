using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Services.Travel;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE-S4a — the pure travel-pricing validation core. Descriptive metadata; the tests assert the fail-loud contract
/// (<c>SR_TRAVEL_*</c>): a valid PerKm (km × rate) and FlatMobilization pass and never re-price the line; every inconsistency
/// (quantity/rate/method mismatch, km on Flat, detail on a non-Travel line, a non-KILOMETER unit) is rejected.
/// </summary>
public sealed class TravelPricingValidationTests
{
    // 40 km × ₺25 PerKm travel line (line Quantity=40, UnitPrice=25, PricingMethod=PerKm, KILOMETER unit resolved).
    private static void PerKm(
        decimal lineQty = 40m, decimal lineUnit = 25m, PricingMethod linePm = PricingMethod.PerKm,
        decimal? distanceKm = 40m, decimal? perKmRate = 25m, string? unit = "KILOMETER", bool kmResolved = true,
        ServiceRequestOfferItemType itemType = ServiceRequestOfferItemType.Travel)
        => TravelPricingValidation.Validate(itemType, lineQty, lineUnit, linePm,
            TravelPricingMethod.PerKm, distanceKm, perKmRate, unit, kmResolved);

    // ₺750 flat mobilization (line Quantity=1, UnitPrice=750, PricingMethod=Fixed, no km/rate/unit).
    private static void Flat(
        decimal lineQty = 1m, decimal lineUnit = 750m, PricingMethod linePm = PricingMethod.Fixed,
        decimal? distanceKm = null, decimal? perKmRate = null, string? unit = null,
        ServiceRequestOfferItemType itemType = ServiceRequestOfferItemType.Travel)
        => TravelPricingValidation.Validate(itemType, lineQty, lineUnit, linePm,
            TravelPricingMethod.FlatMobilization, distanceKm, perKmRate, unit, kilometerUnitResolved: false);

    // ── Valid derivations pass ────────────────────────────────────────────────────
    [Fact] public void PerKm_ConsistentWithLine_Passes() => FluentActions.Invoking(() => PerKm()).Should().NotThrow();
    [Fact] public void Flat_ConsistentWithLine_Passes()  => FluentActions.Invoking(() => Flat()).Should().NotThrow();

    // ── Non-Travel line ───────────────────────────────────────────────────────────
    [Fact]
    public void TravelDetail_OnNonTravelLine_Throws()
        => FluentActions.Invoking(() => PerKm(itemType: ServiceRequestOfferItemType.Service))
            .Should().Throw<AizenBusinessException>().WithMessage("*SR_TRAVEL_NOT_TRAVEL_LINE*");

    // ── PerKm consistency ─────────────────────────────────────────────────────────
    [Fact]
    public void PerKm_QuantityMismatch_Throws()
        => FluentActions.Invoking(() => PerKm(lineQty: 41m))   // line 41 != distanceKm 40
            .Should().Throw<AizenBusinessException>().WithMessage("*SR_TRAVEL_QUANTITY_MISMATCH*");

    [Fact]
    public void PerKm_RateMismatch_Throws()
        => FluentActions.Invoking(() => PerKm(lineUnit: 26m))  // line 26 != perKmRate 25
            .Should().Throw<AizenBusinessException>().WithMessage("*SR_TRAVEL_RATE_MISMATCH*");

    [Fact]
    public void PerKm_NonKilometerUnit_Throws()
        => FluentActions.Invoking(() => PerKm(unit: "LITER", kmResolved: false))
            .Should().Throw<AizenBusinessException>().WithMessage("*SR_TRAVEL_UNIT_NOT_KILOMETER*");

    [Fact]
    public void PerKm_NonPositiveDistance_Throws()
        => FluentActions.Invoking(() => PerKm(lineQty: 0m, distanceKm: 0m))
            .Should().Throw<AizenBusinessException>().WithMessage("*SR_TRAVEL_DISTANCE_REQUIRED*");

    [Fact]
    public void PerKm_LineMethodNotPerKm_Throws()
        => FluentActions.Invoking(() => PerKm(linePm: PricingMethod.Fixed))
            .Should().Throw<AizenBusinessException>().WithMessage("*SR_TRAVEL_METHOD_MISMATCH*");

    // ── Flat consistency ──────────────────────────────────────────────────────────
    [Fact]
    public void Flat_WithDistance_Throws()
        => FluentActions.Invoking(() => Flat(distanceKm: 10m))
            .Should().Throw<AizenBusinessException>().WithMessage("*SR_TRAVEL_FLAT_NO_KM*");

    [Fact]
    public void Flat_WithRate_Throws()
        => FluentActions.Invoking(() => Flat(perKmRate: 5m))
            .Should().Throw<AizenBusinessException>().WithMessage("*SR_TRAVEL_FLAT_NO_RATE*");

    [Fact]
    public void Flat_QuantityNotOne_Throws()
        => FluentActions.Invoking(() => Flat(lineQty: 2m))
            .Should().Throw<AizenBusinessException>().WithMessage("*SR_TRAVEL_FLAT_QUANTITY*");

    [Fact]
    public void Flat_LineMethodNotFixed_Throws()
        => FluentActions.Invoking(() => Flat(linePm: PricingMethod.PerKm))
            .Should().Throw<AizenBusinessException>().WithMessage("*SR_TRAVEL_METHOD_MISMATCH*");
}
