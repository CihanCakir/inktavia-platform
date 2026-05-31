using Aizen.Modules.ReferenceData.Domain.Documents.Location;
using Aizen.Modules.ReferenceData.Domain.Interface;
using MongoDB.Driver;

namespace Aizen.Modules.ReferenceData.Repository.Mongo;

public sealed class LocationRepository : ILocationRepository
{
    private readonly IMongoCollection<LocationCountryDocument> _countries;
    private readonly IMongoCollection<LocationCityDocument> _cities;
    private readonly IMongoCollection<LocationDistrictDocument> _districts;
    private readonly IMongoCollection<LocationNeighborhoodDocument> _neighborhoods;
    private readonly IMongoCollection<LocationStreetDocument> _streets;

    public LocationRepository(IMongoDatabase mongoDatabase)
    {
        _countries = mongoDatabase.GetCollection<LocationCountryDocument>(ReferenceDataMongoCollectionNames.Countries);
        _cities = mongoDatabase.GetCollection<LocationCityDocument>(ReferenceDataMongoCollectionNames.Cities);
        _districts = mongoDatabase.GetCollection<LocationDistrictDocument>(ReferenceDataMongoCollectionNames.Districts);
        _neighborhoods = mongoDatabase.GetCollection<LocationNeighborhoodDocument>(ReferenceDataMongoCollectionNames.Neighborhoods);
        _streets = mongoDatabase.GetCollection<LocationStreetDocument>(ReferenceDataMongoCollectionNames.Streets);
    }

    public async Task<IReadOnlyList<LocationCountryDocument>> GetCountriesAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var filter = onlyActive
            ? Builders<LocationCountryDocument>.Filter.Eq(x => x.IsActive, true)
            : Builders<LocationCountryDocument>.Filter.Empty;

        return await _countries.Find(filter).SortBy(x => x.CountryCode).ToListAsync(cancellationToken);
    }

    public Task<LocationCountryDocument?> GetCountryAsync(string countryCode, CancellationToken cancellationToken = default)
        => _countries.Find(x => x.CountryCode == countryCode.Trim().ToUpperInvariant()).FirstOrDefaultAsync(cancellationToken);

    public Task UpsertCountryAsync(LocationCountryDocument document, CancellationToken cancellationToken = default)
    {
        document.CountryCode = document.CountryCode.Trim().ToUpperInvariant();

        return _countries.ReplaceOneAsync(
            x => x.CountryCode == document.CountryCode,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<IReadOnlyList<LocationCityDocument>> GetCitiesByCountryAsync(string countryCode, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var filter = Builders<LocationCityDocument>.Filter.Eq(x => x.CountryCode, countryCode.Trim().ToUpperInvariant());
        if (onlyActive) filter &= Builders<LocationCityDocument>.Filter.Eq(x => x.IsActive, true);
        return await _cities.Find(filter).SortBy(x => x.CityCode).ToListAsync(cancellationToken);
    }

    public Task<LocationCityDocument?> GetCityAsync(string countryCode, string cityCode, CancellationToken cancellationToken = default)
        => _cities.Find(x => x.CountryCode == countryCode.Trim().ToUpperInvariant() && x.CityCode == cityCode.Trim()).FirstOrDefaultAsync(cancellationToken);

    public Task UpsertCityAsync(LocationCityDocument document, CancellationToken cancellationToken = default)
    {
        document.CountryCode = document.CountryCode.Trim().ToUpperInvariant();
        document.CityCode = document.CityCode.Trim();

        return _cities.ReplaceOneAsync(
            x => x.CountryCode == document.CountryCode && x.CityCode == document.CityCode,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<IReadOnlyList<LocationDistrictDocument>> GetDistrictsByCityAsync(string countryCode, string cityCode, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var filter = Builders<LocationDistrictDocument>.Filter.Eq(x => x.CountryCode, countryCode.Trim().ToUpperInvariant()) &
                     Builders<LocationDistrictDocument>.Filter.Eq(x => x.CityCode, cityCode.Trim());

        if (onlyActive) filter &= Builders<LocationDistrictDocument>.Filter.Eq(x => x.IsActive, true);
        return await _districts.Find(filter).SortBy(x => x.DistrictCode).ToListAsync(cancellationToken);
    }

    public Task<LocationDistrictDocument?> GetDistrictAsync(string countryCode, string cityCode, string districtCode, CancellationToken cancellationToken = default)
        => _districts.Find(x => x.CountryCode == countryCode.Trim().ToUpperInvariant() && x.CityCode == cityCode.Trim() && x.DistrictCode == districtCode.Trim()).FirstOrDefaultAsync(cancellationToken);

    public Task UpsertDistrictAsync(LocationDistrictDocument document, CancellationToken cancellationToken = default)
    {
        document.CountryCode = document.CountryCode.Trim().ToUpperInvariant();
        document.CityCode = document.CityCode.Trim();
        document.DistrictCode = document.DistrictCode.Trim();

        return _districts.ReplaceOneAsync(
            x => x.CountryCode == document.CountryCode && x.CityCode == document.CityCode && x.DistrictCode == document.DistrictCode,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<IReadOnlyList<LocationNeighborhoodDocument>> GetNeighborhoodsByDistrictAsync(string countryCode, string cityCode, string districtCode, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var filter = Builders<LocationNeighborhoodDocument>.Filter.Eq(x => x.CountryCode, countryCode.Trim().ToUpperInvariant()) &
                     Builders<LocationNeighborhoodDocument>.Filter.Eq(x => x.CityCode, cityCode.Trim()) &
                     Builders<LocationNeighborhoodDocument>.Filter.Eq(x => x.DistrictCode, districtCode.Trim());

        if (onlyActive) filter &= Builders<LocationNeighborhoodDocument>.Filter.Eq(x => x.IsActive, true);
        return await _neighborhoods.Find(filter).SortBy(x => x.NeighborhoodCode).ToListAsync(cancellationToken);
    }

    public Task UpsertNeighborhoodAsync(LocationNeighborhoodDocument document, CancellationToken cancellationToken = default)
    {
        document.CountryCode = document.CountryCode.Trim().ToUpperInvariant();
        document.CityCode = document.CityCode.Trim();
        document.DistrictCode = document.DistrictCode.Trim();
        document.NeighborhoodCode = document.NeighborhoodCode.Trim();

        return _neighborhoods.ReplaceOneAsync(
            x => x.CountryCode == document.CountryCode && x.CityCode == document.CityCode && x.DistrictCode == document.DistrictCode && x.NeighborhoodCode == document.NeighborhoodCode,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<IReadOnlyList<LocationStreetDocument>> GetStreetsByNeighborhoodAsync(string countryCode, string cityCode, string districtCode, string? neighborhoodCode, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var filter = Builders<LocationStreetDocument>.Filter.Eq(x => x.CountryCode, countryCode.Trim().ToUpperInvariant()) &
                     Builders<LocationStreetDocument>.Filter.Eq(x => x.CityCode, cityCode.Trim()) &
                     Builders<LocationStreetDocument>.Filter.Eq(x => x.DistrictCode, districtCode.Trim());

        if (!string.IsNullOrWhiteSpace(neighborhoodCode))
            filter &= Builders<LocationStreetDocument>.Filter.Eq(x => x.NeighborhoodCode, neighborhoodCode.Trim());

        if (onlyActive)
            filter &= Builders<LocationStreetDocument>.Filter.Eq(x => x.IsActive, true);

        return await _streets.Find(filter).SortBy(x => x.StreetCode).ToListAsync(cancellationToken);
    }

    public Task UpsertStreetAsync(LocationStreetDocument document, CancellationToken cancellationToken = default)
    {
        document.CountryCode = document.CountryCode.Trim().ToUpperInvariant();
        document.CityCode = document.CityCode.Trim();
        document.DistrictCode = document.DistrictCode.Trim();
        document.NeighborhoodCode = document.NeighborhoodCode?.Trim();
        document.StreetCode = document.StreetCode.Trim();

        return _streets.ReplaceOneAsync(
            x => x.CountryCode == document.CountryCode &&
                 x.CityCode == document.CityCode &&
                 x.DistrictCode == document.DistrictCode &&
                 x.NeighborhoodCode == document.NeighborhoodCode &&
                 x.StreetCode == document.StreetCode,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }
}
