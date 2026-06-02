using Aizen.Core.Data.Mongo;
using Aizen.Core.Data.Mongo.Document;
using Aizen.Core.Data.Mongo.Repository;
using Aizen.Modules.ReferenceData.Domain.Documents.Location;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using MongoDB.Driver.Linq;

namespace Aizen.Modules.ReferenceData.Repository.Mongo;

public sealed class LocationRepository : ILocationRepository
{
    private readonly IAizenMongoRepository<LocationCountryDocument> _countries;
    private readonly IAizenMongoRepository<LocationCityDocument> _cities;
    private readonly IAizenMongoRepository<LocationDistrictDocument> _districts;
    private readonly IAizenMongoRepository<LocationNeighborhoodDocument> _neighborhoods;
    private readonly IAizenMongoRepository<LocationStreetDocument> _streets;

    public LocationRepository(IAizenMongoRepositoryFactory<ReferenceDataMongoDbContext> aizenMongoRepositoryFactory)
    {
        _countries = aizenMongoRepositoryFactory.GetRepository<LocationCountryDocument>();
        _cities = aizenMongoRepositoryFactory.GetRepository<LocationCityDocument>();
        _districts = aizenMongoRepositoryFactory.GetRepository<LocationDistrictDocument>();
        _neighborhoods = aizenMongoRepositoryFactory.GetRepository<LocationNeighborhoodDocument>();
        _streets = aizenMongoRepositoryFactory.GetRepository<LocationStreetDocument>();
    }

    public Task<bool> AnyCountryAsync(CancellationToken cancellationToken = default)
        => _countries.AnyAsync(null, cancellationToken);

    public async Task<IReadOnlyList<LocationCountryDocument>> GetCountriesAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var result = await _countries.FindManyAsync(
            predicate: onlyActive ? x => x.IsActive : null,
            orderBy: q => (IOrderedMongoQueryable<LocationCountryDocument>)q.OrderBy(x => x.CountryCode),
            topCount: -1,
            cancellationToken: cancellationToken);
        return (IReadOnlyList<LocationCountryDocument>)result;
    }

    public Task<LocationCountryDocument?> GetCountryAsync(string countryCode, CancellationToken cancellationToken = default)
    {
        var normalized = countryCode.Trim().ToUpperInvariant();
        return _countries.FindAsync(x => x.CountryCode == normalized, cancellationToken: cancellationToken)!;
    }

    public async Task UpsertCountryAsync(LocationCountryDocument document, CancellationToken cancellationToken = default)
    {
        document.CountryCode = document.CountryCode.Trim().ToUpperInvariant();

        var existing = await _countries.FindAsync(x => x.CountryCode == document.CountryCode, cancellationToken: cancellationToken);
        if (existing != null)
        {
            document.Id = existing.Id;
            await _countries.ReplaceAsync(document, cancellationToken);
        }
        else
        {
            await _countries.AddAsync(document, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<LocationCityDocument>> GetCitiesByCountryAsync(string countryCode, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var normalized = countryCode.Trim().ToUpperInvariant();
        var result = await _cities.FindManyAsync(
            predicate: onlyActive
                ? x => x.CountryCode == normalized && x.IsActive
                : x => x.CountryCode == normalized,
            orderBy: q => (IOrderedMongoQueryable<LocationCityDocument>)q.OrderBy(x => x.CityCode),
            topCount: -1,
            cancellationToken: cancellationToken);
        return (IReadOnlyList<LocationCityDocument>)result;
    }

    public Task<LocationCityDocument?> GetCityAsync(string countryCode, string cityCode, CancellationToken cancellationToken = default)
    {
        var normalizedCountry = countryCode.Trim().ToUpperInvariant();
        var normalizedCity = cityCode.Trim();
        return _cities.FindAsync(x => x.CountryCode == normalizedCountry && x.CityCode == normalizedCity, cancellationToken: cancellationToken)!;
    }

    public async Task UpsertCityAsync(LocationCityDocument document, CancellationToken cancellationToken = default)
    {
        document.CountryCode = document.CountryCode.Trim().ToUpperInvariant();
        document.CityCode = document.CityCode.Trim();

        var existing = await _cities.FindAsync(
            x => x.CountryCode == document.CountryCode && x.CityCode == document.CityCode,
            cancellationToken: cancellationToken);
        if (existing != null)
        {
            document.Id = existing.Id;
            await _cities.ReplaceAsync(document, cancellationToken);
        }
        else
        {
            await _cities.AddAsync(document, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<LocationDistrictDocument>> GetDistrictsByCityAsync(string countryCode, string cityCode, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var normalizedCountry = countryCode.Trim().ToUpperInvariant();
        var normalizedCity = cityCode.Trim();
        var result = await _districts.FindManyAsync(
            predicate: onlyActive
                ? x => x.CountryCode == normalizedCountry && x.CityCode == normalizedCity && x.IsActive
                : x => x.CountryCode == normalizedCountry && x.CityCode == normalizedCity,
            orderBy: q => (IOrderedMongoQueryable<LocationDistrictDocument>)q.OrderBy(x => x.DistrictCode),
            topCount: -1,
            cancellationToken: cancellationToken);
        return (IReadOnlyList<LocationDistrictDocument>)result;
    }

    public Task<LocationDistrictDocument?> GetDistrictAsync(string countryCode, string cityCode, string districtCode, CancellationToken cancellationToken = default)
    {
        var normalizedCountry = countryCode.Trim().ToUpperInvariant();
        var normalizedCity = cityCode.Trim();
        var normalizedDistrict = districtCode.Trim();
        return _districts.FindAsync(
            x => x.CountryCode == normalizedCountry && x.CityCode == normalizedCity && x.DistrictCode == normalizedDistrict,
            cancellationToken: cancellationToken)!;
    }

    public async Task UpsertDistrictAsync(LocationDistrictDocument document, CancellationToken cancellationToken = default)
    {
        document.CountryCode = document.CountryCode.Trim().ToUpperInvariant();
        document.CityCode = document.CityCode.Trim();
        document.DistrictCode = document.DistrictCode.Trim();

        var existing = await _districts.FindAsync(
            x => x.CountryCode == document.CountryCode && x.CityCode == document.CityCode && x.DistrictCode == document.DistrictCode,
            cancellationToken: cancellationToken);
        if (existing != null)
        {
            document.Id = existing.Id;
            await _districts.ReplaceAsync(document, cancellationToken);
        }
        else
        {
            await _districts.AddAsync(document, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<LocationNeighborhoodDocument>> GetNeighborhoodsByDistrictAsync(string countryCode, string cityCode, string districtCode, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var normalizedCountry = countryCode.Trim().ToUpperInvariant();
        var normalizedCity = cityCode.Trim();
        var normalizedDistrict = districtCode.Trim();
        var result = await _neighborhoods.FindManyAsync(
            predicate: onlyActive
                ? x => x.CountryCode == normalizedCountry && x.CityCode == normalizedCity && x.DistrictCode == normalizedDistrict && x.IsActive
                : x => x.CountryCode == normalizedCountry && x.CityCode == normalizedCity && x.DistrictCode == normalizedDistrict,
            orderBy: q => (IOrderedMongoQueryable<LocationNeighborhoodDocument>)q.OrderBy(x => x.NeighborhoodCode),
            topCount: -1,
            cancellationToken: cancellationToken);
        return (IReadOnlyList<LocationNeighborhoodDocument>)result;
    }

    public async Task UpsertNeighborhoodAsync(LocationNeighborhoodDocument document, CancellationToken cancellationToken = default)
    {
        document.CountryCode = document.CountryCode.Trim().ToUpperInvariant();
        document.CityCode = document.CityCode.Trim();
        document.DistrictCode = document.DistrictCode.Trim();
        document.NeighborhoodCode = document.NeighborhoodCode.Trim();

        var existing = await _neighborhoods.FindAsync(
            x => x.CountryCode == document.CountryCode && x.CityCode == document.CityCode &&
                 x.DistrictCode == document.DistrictCode && x.NeighborhoodCode == document.NeighborhoodCode,
            cancellationToken: cancellationToken);
        if (existing != null)
        {
            document.Id = existing.Id;
            await _neighborhoods.ReplaceAsync(document, cancellationToken);
        }
        else
        {
            await _neighborhoods.AddAsync(document, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<LocationStreetDocument>> GetStreetsByNeighborhoodAsync(string countryCode, string cityCode, string districtCode, string? neighborhoodCode, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var normalizedCountry = countryCode.Trim().ToUpperInvariant();
        var normalizedCity = cityCode.Trim();
        var normalizedDistrict = districtCode.Trim();
        var normalizedNeighborhood = neighborhoodCode?.Trim();
        var result = await _streets.FindManyAsync(
            predicate: x => x.CountryCode == normalizedCountry &&
                            x.CityCode == normalizedCity &&
                            x.DistrictCode == normalizedDistrict &&
                            (normalizedNeighborhood == null || x.NeighborhoodCode == normalizedNeighborhood) &&
                            (!onlyActive || x.IsActive),
            orderBy: q => (IOrderedMongoQueryable<LocationStreetDocument>)q.OrderBy(x => x.StreetCode),
            topCount: -1,
            cancellationToken: cancellationToken);
        return (IReadOnlyList<LocationStreetDocument>)result;
    }

    public async Task UpsertStreetAsync(LocationStreetDocument document, CancellationToken cancellationToken = default)
    {
        document.CountryCode = document.CountryCode.Trim().ToUpperInvariant();
        document.CityCode = document.CityCode.Trim();
        document.DistrictCode = document.DistrictCode.Trim();
        document.NeighborhoodCode = document.NeighborhoodCode?.Trim();
        document.StreetCode = document.StreetCode.Trim();

        var existing = await _streets.FindAsync(
            x => x.CountryCode == document.CountryCode &&
                 x.CityCode == document.CityCode &&
                 x.DistrictCode == document.DistrictCode &&
                 x.NeighborhoodCode == document.NeighborhoodCode &&
                 x.StreetCode == document.StreetCode,
            cancellationToken: cancellationToken);
        if (existing != null)
        {
            document.Id = existing.Id;
            await _streets.ReplaceAsync(document, cancellationToken);
        }
        else
        {
            await _streets.AddAsync(document, cancellationToken);
        }
    }
}
