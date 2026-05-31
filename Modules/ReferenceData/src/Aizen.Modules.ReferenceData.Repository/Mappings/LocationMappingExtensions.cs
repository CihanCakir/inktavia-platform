using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Documents.Location;

namespace Aizen.Modules.ReferenceData.Repository.Mappings;

public static class LocationMappingExtensions
{
    public static CountryDto ToDto(this LocationCountryDocument doc) => new()
    {
        CountryCode = doc.CountryCode,
        NumericCode = doc.NumericCode,
        Name = doc.Name.GetValueOrDefault("en") ?? doc.Name.Values.FirstOrDefault() ?? doc.CountryCode,
        DefaultCurrencyCode = doc.DefaultCurrencyCode,
        PhoneCode = doc.PhoneCode,
        IsActive = doc.IsActive
    };

    public static CityDto ToDto(this LocationCityDocument doc) => new()
    {
        CountryCode = doc.CountryCode,
        CityCode = doc.CityCode,
        Name = doc.Name.GetValueOrDefault("en") ?? doc.Name.Values.FirstOrDefault() ?? doc.CityCode,
        Latitude = doc.Latitude,
        Longitude = doc.Longitude,
        IsCoastalCity = doc.IsCoastalCity,
        IsActive = doc.IsActive
    };

    public static DistrictDto ToDto(this LocationDistrictDocument doc) => new()
    {
        CountryCode = doc.CountryCode,
        CityCode = doc.CityCode,
        DistrictCode = doc.DistrictCode,
        Name = doc.Name.GetValueOrDefault("en") ?? doc.Name.Values.FirstOrDefault() ?? doc.DistrictCode,
        Latitude = doc.Latitude,
        Longitude = doc.Longitude,
        IsCoastalDistrict = doc.IsCoastalDistrict,
        IsActive = doc.IsActive
    };

    public static NeighborhoodDto ToDto(this LocationNeighborhoodDocument doc) => new()
    {
        CountryCode = doc.CountryCode,
        CityCode = doc.CityCode,
        DistrictCode = doc.DistrictCode,
        NeighborhoodCode = doc.NeighborhoodCode,
        Name = doc.Name.GetValueOrDefault("en") ?? doc.Name.Values.FirstOrDefault() ?? doc.NeighborhoodCode,
        PostalCode = doc.PostalCode,
        IsActive = doc.IsActive
    };

    public static StreetDto ToDto(this LocationStreetDocument doc) => new()
    {
        CountryCode = doc.CountryCode,
        CityCode = doc.CityCode,
        DistrictCode = doc.DistrictCode,
        NeighborhoodCode = doc.NeighborhoodCode,
        StreetCode = doc.StreetCode,
        Name = doc.Name.GetValueOrDefault("en") ?? doc.Name.Values.FirstOrDefault() ?? doc.StreetCode,
        PostalCode = doc.PostalCode,
        IsActive = doc.IsActive
    };
}
