using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Localization;
using Aizen.Modules.ReferenceData.Domain.Documents.Location;

namespace Aizen.Modules.ReferenceData.Repository.Mappings;

// FAZ12B #49: Ad artık SABİT "en" ile değil, ÇAĞIRANIN DİLİYLE çözülüyor (LocationNameResolver).
// `language` = Accept-Language başlığı (IAizenClientInfoAccessor.ClientInfo.Language); LocationReferenceService
// bunu aktarır. Verilmezse (arka plan/seed) null → "en" yedeğine düşer (eski davranış korunur).
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
