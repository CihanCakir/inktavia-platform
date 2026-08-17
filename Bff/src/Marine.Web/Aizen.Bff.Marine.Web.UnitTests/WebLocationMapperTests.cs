using System.Reflection;
using Aizen.Bff.Marine.Web.Application.Contracts.Location;
using Aizen.Bff.Marine.Web.Application.Location;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using FluentAssertions;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// W5 — the W4 location-detail mapper. Verifies the parent chain is built per level, the coastal flag is carried,
/// a missing parent record falls back to the CODE (never a fabricated name), and the web DTO carries no internal ids.
/// </summary>
public sealed class WebLocationMapperTests
{
    private static string[] Props<T>() => typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(p => p.Name).ToArray();

    private static readonly CountryDto Tr = new() { CountryCode = "TR", Name = "Türkiye", IsActive = true };
    private static readonly CityDto Istanbul = new()
    { CountryCode = "TR", CityCode = "34", Name = "İstanbul", IsCoastalCity = true, Latitude = 41.0m, Longitude = 28.9m };
    private static readonly DistrictDto Kadikoy = new()
    { CountryCode = "TR", CityCode = "34", DistrictCode = "KADIKOY", Name = "Kadıköy", IsCoastalDistrict = true };

    [Fact]
    public void Country_detail_has_empty_parent_chain()
    {
        var d = WebLocationMapper.ToCountryDetail(Tr);
        d.LocationType.Should().Be("country");
        d.Code.Should().Be("TR");
        d.Name.Should().Be("Türkiye");
        d.ParentChain.Should().BeEmpty();
    }

    [Fact]
    public void City_detail_carries_country_parent_and_coastal_flag()
    {
        var d = WebLocationMapper.ToCityDetail(Istanbul, Tr);
        d.LocationType.Should().Be("city");
        d.Code.Should().Be("34");
        d.IsCoastal.Should().BeTrue();
        d.Latitude.Should().Be(41.0m);
        d.ParentChain.Should().ContainSingle();
        d.ParentChain[0].Should().Match<WebLocationRefDto>(p =>
            p.LocationType == "country" && p.Code == "TR" && p.Name == "Türkiye");
    }

    [Fact]
    public void District_detail_carries_country_then_city_chain()
    {
        var d = WebLocationMapper.ToDistrictDetail(Kadikoy, Istanbul, Tr);
        d.LocationType.Should().Be("district");
        d.ParentChain.Select(p => (p.LocationType, p.Code, p.Name))
            .Should().Equal(("country", "TR", "Türkiye"), ("city", "34", "İstanbul"));
    }

    [Fact]
    public void Parent_name_falls_back_to_code_when_parent_unresolved()
    {
        // Parent records not resolvable (e.g. a downstream hiccup) → the chain stays structurally complete with the
        // real CODE as the display value, never an invented name.
        var city = WebLocationMapper.ToCityDetail(Istanbul, country: null);
        city.ParentChain.Single().Name.Should().Be("TR");

        var district = WebLocationMapper.ToDistrictDetail(Kadikoy, city: null, country: null);
        district.ParentChain.Select(p => p.Name).Should().Equal("TR", "34");
    }

    [Fact]
    public void WebLocationDetailDto_carries_no_internal_ids()
    {
        var names = Props<WebLocationDetailDto>();
        names.Should().NotContain("Id");
        names.Should().NotContain("CountryCode", "the location's own code is carried as Code; parents live in ParentChain");
        names.Should().NotContain("IsActive");
    }
}
