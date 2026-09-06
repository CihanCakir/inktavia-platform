using System.Text.Json;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Repository.Identity.Service.Onboarding;
using FluentAssertions;

namespace Aizen.Modules.Identity.Repository.UnitTests.Onboarding;

public sealed class OnboardingCoordinateRulesTests
{
    private static JsonElement Region(string json) => JsonDocument.Parse(json).RootElement;

    // ── Validate (save-time, validate-when-present) ──────────────────────────

    [Fact]
    public void Validate_Passes_When_No_Coordinates()
    {
        var act = () => OnboardingCoordinateRules.Validate(Region("""{ "cityCode": "34", "country": "TR" }"""));
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Passes_For_A_Turkey_Point()
    {
        // İstanbul-ish
        var act = () => OnboardingCoordinateRules.Validate(Region("""{ "businessLatitude": 40.9739, "businessLongitude": 29.0355 }"""));
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Throws_When_Only_Latitude_Present()
    {
        var act = () => OnboardingCoordinateRules.Validate(Region("""{ "businessLatitude": 40.97 }"""));
        act.Should().Throw<AizenBusinessException>().WithMessage("*provided together*");
    }

    [Fact]
    public void Validate_Throws_When_Only_Longitude_Present()
    {
        var act = () => OnboardingCoordinateRules.Validate(Region("""{ "businessLongitude": 29.03 }"""));
        act.Should().Throw<AizenBusinessException>().WithMessage("*provided together*");
    }

    [Fact]
    public void Validate_Throws_When_Outside_Turkey_Bbox()
    {
        // London — valid global coords, but outside the Turkey plausibility box.
        var act = () => OnboardingCoordinateRules.Validate(Region("""{ "businessLatitude": 51.5, "businessLongitude": -0.12 }"""));
        act.Should().Throw<AizenBusinessException>().WithMessage("*outside the supported region*");
    }

    // ── TryReadBusinessCoords (materialization) ──────────────────────────────

    [Fact]
    public void TryRead_Returns_Coords_And_Label()
    {
        var ok = OnboardingCoordinateRules.TryReadBusinessCoords(
            Region("""{ "businessLatitude": 37.0320, "businessLongitude": 27.4270, "businessAddressLabel": "Bodrum ofis" }"""),
            out var lat, out var lng, out var label);

        ok.Should().BeTrue();
        lat.Should().Be(37.0320m);
        lng.Should().Be(27.4270m);
        label.Should().Be("Bodrum ofis");
    }

    [Fact]
    public void TryRead_Returns_False_When_A_Coord_Is_Missing()
    {
        OnboardingCoordinateRules.TryReadBusinessCoords(
            Region("""{ "businessLatitude": 37.0 }"""), out _, out _, out _)
            .Should().BeFalse();
    }

    [Fact]
    public void TryRead_Returns_False_On_Out_Of_Global_Range()
    {
        OnboardingCoordinateRules.TryReadBusinessCoords(
            Region("""{ "businessLatitude": 999, "businessLongitude": 999 }"""), out _, out _, out _)
            .Should().BeFalse();
    }
}
