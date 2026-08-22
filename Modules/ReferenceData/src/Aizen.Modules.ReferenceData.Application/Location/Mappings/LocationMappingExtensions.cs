using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Localization;
using Aizen.Modules.ReferenceData.Domain.Documents.Location;

namespace Aizen.Modules.ReferenceData.Application.Location.Mappings;

// FAZ12B #49: Ad artık SABİT "en" ile değil, ÇAĞIRANIN DİLİYLE çözülüyor (LocationNameResolver).
// NOT: bu Application kopyası şu an hiçbir yerden çağrılmıyor (çalışan yol Repository/LocationReferenceService);
// yine de tutarlılık + sabit-"en" temizliği için aynı desene alındı. `language` verilmezse "en" yedeği.
public static class LocationMappingExtensions
{
    public static CountryDto ToDto(this LocationCountryDocument doc, string? language = null) => new()
    {
        CountryCode = doc.CountryCode,
        NumericCode = doc.NumericCode,
        Name = LocationNameResolver.Resolve(doc.Name, doc.CountryCode, language),
        DefaultCurrencyCode = doc.DefaultCurrencyCode,
        PhoneCode = doc.PhoneCode,
        IsActive = doc.IsActive
    };

    public static CityDto ToDto(this LocationCityDocument doc, string? language = null) => new()
    {
        CountryCode = doc.CountryCode,
        CityCode = doc.CityCode,
        Name = LocationNameResolver.Resolve(doc.Name, doc.CityCode, language),
        Latitude = doc.Latitude,
        Longitude = doc.Longitude,
        IsCoastalCity = doc.IsCoastalCity,
        IsActive = doc.IsActive
    };

    public static DistrictDto ToDto(this LocationDistrictDocument doc, string? language = null) => new()
    {
        CountryCode = doc.CountryCode,
        CityCode = doc.CityCode,
        DistrictCode = doc.DistrictCode,
        Name = LocationNameResolver.Resolve(doc.Name, doc.DistrictCode, language),
        Latitude = doc.Latitude,
        Longitude = doc.Longitude,
        IsCoastalDistrict = doc.IsCoastalDistrict,
        IsActive = doc.IsActive
    };

    public static NeighborhoodDto ToDto(this LocationNeighborhoodDocument doc, string? language = null) => new()
    {
        CountryCode = doc.CountryCode,
        CityCode = doc.CityCode,
        DistrictCode = doc.DistrictCode,
        NeighborhoodCode = doc.NeighborhoodCode,
        Name = LocationNameResolver.Resolve(doc.Name, doc.NeighborhoodCode, language),
        PostalCode = doc.PostalCode,
        IsActive = doc.IsActive
    };

    public static StreetDto ToDto(this LocationStreetDocument doc, string? language = null) => new()
    {
        CountryCode = doc.CountryCode,
        CityCode = doc.CityCode,
        DistrictCode = doc.DistrictCode,
        NeighborhoodCode = doc.NeighborhoodCode,
        StreetCode = doc.StreetCode,
        Name = LocationNameResolver.Resolve(doc.Name, doc.StreetCode, language),
        PostalCode = doc.PostalCode,
        IsActive = doc.IsActive
    };
}
