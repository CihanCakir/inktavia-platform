using Aizen.Modules.ReferenceData.Domain.Documents.Location;

namespace Aizen.Modules.ReferenceData.Domain.Interface;

public interface ILocationRepository
{
    Task<IReadOnlyList<LocationCountryDocument>> GetCountriesAsync(bool onlyActive, CancellationToken cancellationToken = default);
    Task<LocationCountryDocument?> GetCountryAsync(string countryCode, CancellationToken cancellationToken = default);
    Task UpsertCountryAsync(LocationCountryDocument document, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocationCityDocument>> GetCitiesByCountryAsync(string countryCode, bool onlyActive, CancellationToken cancellationToken = default);
    Task<LocationCityDocument?> GetCityAsync(string countryCode, string cityCode, CancellationToken cancellationToken = default);
    Task UpsertCityAsync(LocationCityDocument document, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocationDistrictDocument>> GetDistrictsByCityAsync(string countryCode, string cityCode, bool onlyActive, CancellationToken cancellationToken = default);
    Task<LocationDistrictDocument?> GetDistrictAsync(string countryCode, string cityCode, string districtCode, CancellationToken cancellationToken = default);
    Task UpsertDistrictAsync(LocationDistrictDocument document, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocationNeighborhoodDocument>> GetNeighborhoodsByDistrictAsync(string countryCode, string cityCode, string districtCode, bool onlyActive, CancellationToken cancellationToken = default);
    Task UpsertNeighborhoodAsync(LocationNeighborhoodDocument document, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocationStreetDocument>> GetStreetsByNeighborhoodAsync(string countryCode, string cityCode, string districtCode, string? neighborhoodCode, bool onlyActive, CancellationToken cancellationToken = default);
    Task UpsertStreetAsync(LocationStreetDocument document, CancellationToken cancellationToken = default);

    Task<bool> AnyCountryAsync(CancellationToken cancellationToken = default);
}
