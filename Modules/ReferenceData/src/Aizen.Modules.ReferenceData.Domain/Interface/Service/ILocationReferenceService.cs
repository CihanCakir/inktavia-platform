using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Request.Location;

namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

public interface ILocationReferenceService
{
    Task<CountryDto> CreateCountryAsync(CreateCountryRequest request, CancellationToken cancellationToken = default);
    Task<CountryDto> UpdateCountryAsync(string countryCode, string defaultCurrencyCode, string phoneCode, bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CountryDto>> GetCountriesAsync(bool onlyActive, CancellationToken cancellationToken = default);
    Task<CountryDto?> GetCountryAsync(string countryCode, CancellationToken cancellationToken = default);

    Task<CityDto> CreateCityAsync(CreateCityRequest request, CancellationToken cancellationToken = default);
    Task<CityDto> UpdateCityAsync(string countryCode, string cityCode, decimal? latitude, decimal? longitude, bool isCoastalCity, bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CityDto>> GetCitiesByCountryAsync(string countryCode, bool onlyActive, CancellationToken cancellationToken = default);
    Task<CityDto?> GetCityAsync(string countryCode, string cityCode, CancellationToken cancellationToken = default);

    Task<DistrictDto> CreateDistrictAsync(string countryCode, string cityCode, string districtCode, Dictionary<string, string> name, decimal? latitude, decimal? longitude, bool isCoastalDistrict, CancellationToken cancellationToken = default);
    Task<DistrictDto> UpdateDistrictAsync(string countryCode, string cityCode, string districtCode, decimal? latitude, decimal? longitude, bool isCoastalDistrict, bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DistrictDto>> GetDistrictsByCityAsync(string countryCode, string cityCode, bool onlyActive, CancellationToken cancellationToken = default);
    Task<DistrictDto?> GetDistrictAsync(string countryCode, string cityCode, string districtCode, CancellationToken cancellationToken = default);

    Task<NeighborhoodDto> CreateNeighborhoodAsync(string countryCode, string cityCode, string districtCode, string neighborhoodCode, Dictionary<string, string> name, string? postalCode, CancellationToken cancellationToken = default);
    Task<NeighborhoodDto> UpdateNeighborhoodAsync(string countryCode, string cityCode, string districtCode, string neighborhoodCode, string? postalCode, bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NeighborhoodDto>> GetNeighborhoodsByDistrictAsync(string countryCode, string cityCode, string districtCode, bool onlyActive, CancellationToken cancellationToken = default);
    Task<NeighborhoodDto?> GetNeighborhoodAsync(string countryCode, string cityCode, string districtCode, string neighborhoodCode, CancellationToken cancellationToken = default);

    // M3 — flat by-slug resolver: returns the location + its ancestor chain, or null when no slug matches.
    Task<LocationBySlugDto?> ResolveBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<StreetDto> CreateStreetAsync(string countryCode, string cityCode, string districtCode, string? neighborhoodCode, string streetCode, Dictionary<string, string> name, string? postalCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StreetDto>> GetStreetsByNeighborhoodAsync(string countryCode, string cityCode, string districtCode, string? neighborhoodCode, bool onlyActive, CancellationToken cancellationToken = default);
}
