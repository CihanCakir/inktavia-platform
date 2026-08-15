using Aizen.Bff.Marine.Web.Application.Contracts.Location;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Bff.Marine.Web.Application.Location;

/// <summary>
/// ReferenceData geography DTOs → web location-detail DTO. Builds the parent chain from the (already fetched) parent
/// records. Only code-keyed country/city/district are reachable; region/marina/slug are out of scope (BLOCKED).
/// </summary>
public static class WebLocationMapper
{
    public const string Country = "country";
    public const string City = "city";
    public const string District = "district";

    public static WebLocationDetailDto ToCountryDetail(CountryDto c) => new()
    {
        LocationType = Country,
        Code = c.CountryCode,
        Name = c.Name,
        ParentChain = new(),
        IsCoastal = null,
    };

    public static WebLocationDetailDto ToCityDetail(CityDto city, CountryDto? country) => new()
    {
        LocationType = City,
        Code = city.CityCode,
        Name = city.Name,
        Latitude = city.Latitude,
        Longitude = city.Longitude,
        IsCoastal = city.IsCoastalCity,
        ParentChain = new() { CountryRef(city.CountryCode, country) },
    };

    public static WebLocationDetailDto ToDistrictDetail(DistrictDto d, CityDto? city, CountryDto? country) => new()
    {
        LocationType = District,
        Code = d.DistrictCode,
        Name = d.Name,
        Latitude = d.Latitude,
        Longitude = d.Longitude,
        IsCoastal = d.IsCoastalDistrict,
        ParentChain = new()
        {
            CountryRef(d.CountryCode, country),
            CityRef(d.CityCode, city),
        },
    };

    // Parent refs fall back to the code as the display name when the parent record could not be resolved — the chain
    // stays structurally complete (real codes) without fabricating a name.
    private static WebLocationRefDto CountryRef(string code, CountryDto? country) => new()
    {
        LocationType = Country, Code = code, Name = country?.Name ?? code,
    };

    private static WebLocationRefDto CityRef(string code, CityDto? city) => new()
    {
        LocationType = City, Code = code, Name = city?.Name ?? code,
    };
}
