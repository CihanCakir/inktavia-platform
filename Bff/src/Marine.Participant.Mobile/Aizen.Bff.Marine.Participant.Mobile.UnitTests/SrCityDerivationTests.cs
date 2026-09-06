using Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;
using FluentAssertions;
using Xunit;

namespace Aizen.Bff.Marine.Participant.Mobile.UnitTests;

public sealed class SrCityDerivationTests
{
    [Fact]
    public void Fills_From_Nearest_Marina_When_Within_Cap()
    {
        // ~0.6 km away, cityCode "34" (İstanbul) → derive it.
        SrCityDerivation.ResolveCityCode(currentCityCode: null, nearestCityCode: "34", nearestDistanceMeters: 624)
            .Should().Be("34");
    }

    [Fact]
    public void Fills_At_The_Cap_Boundary()
    {
        SrCityDerivation.ResolveCityCode(null, "35", SrCityDerivation.MaxDeriveDistanceMeters)
            .Should().Be("35");
    }

    [Fact]
    public void Leaves_Null_When_Beyond_Cap()
    {
        // 120 km away — too far to trust as a city proxy.
        SrCityDerivation.ResolveCityCode(null, "34", 120_000)
            .Should().BeNull();
    }

    [Fact]
    public void Never_Overrides_A_Supplied_City()
    {
        // Even a very close marina in a different city must not override an explicitly supplied city.
        SrCityDerivation.ResolveCityCode(currentCityCode: "48", nearestCityCode: "34", nearestDistanceMeters: 10)
            .Should().Be("48");
    }

    [Fact]
    public void Leaves_Null_When_Nearest_Has_No_CityCode()
    {
        SrCityDerivation.ResolveCityCode(null, nearestCityCode: null, nearestDistanceMeters: 100)
            .Should().BeNull();
    }

    [Fact]
    public void Leaves_Null_When_No_Distance()
    {
        SrCityDerivation.ResolveCityCode(null, "34", nearestDistanceMeters: null)
            .Should().BeNull();
    }
}
