using Aizen.Modules.ReferenceData.Abstraction.Model;
using Aizen.Modules.ReferenceData.Domain.Documents.Location;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Seed.Models.Location;
using Aizen.Modules.ReferenceData.Repository.Seed.Readers;

namespace Aizen.Modules.ReferenceData.Repository.Seed.Services;

/// <summary>Seeds location MongoDB documents from JSON files, supporting recursive discovery of district, neighborhood, and street files.</summary>
[DocumentationInfo(
    "Seeds LocationCountry, LocationCity, LocationDistrict, LocationNeighborhood, and LocationStreet documents.",
    "Reads from Location/<ISO2>/ directories recursively. Idempotency is handled by ILocationRepository upsert operations.")]
public sealed class LocationJsonSeedService
{
    private readonly ILocationRepository _locationRepository;
    private readonly IReferenceDataJsonSeedReader _reader;

    public LocationJsonSeedService(
        ILocationRepository locationRepository,
        IReferenceDataJsonSeedReader reader)
    {
        _locationRepository = locationRepository;
        _reader = reader;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedCountryAsync("TR", cancellationToken);
        await SeedCitiesAsync("TR", cancellationToken);
        await SeedAllDistrictsAsync("TR", cancellationToken);
        await SeedAllNeighborhoodsAsync("TR", cancellationToken);
        await SeedAllStreetsAsync("TR", cancellationToken);
    }

    private async Task SeedCountryAsync(string countryIso, CancellationToken cancellationToken)
    {
        var model = await _reader.ReadSingleAsync<LocationCountrySeedModel>(
            $"Location/{countryIso}/country.json",
            optional: true,
            cancellationToken: cancellationToken);

        if (model is null) return;

        var document = new LocationCountryDocument
        {
            CountryCode = model.CountryCode,
            NumericCode = model.NumericCode,
            Name = model.Name,
            DefaultCurrencyCode = model.DefaultCurrencyCode,
            PhoneCode = model.PhoneCode,
            IsActive = model.IsActive
        };

        await _locationRepository.UpsertCountryAsync(document, cancellationToken);
    }

    private async Task SeedCitiesAsync(string countryIso, CancellationToken cancellationToken)
    {
        var models = await _reader.ReadListAsync<LocationCitySeedModel>(
            $"Location/{countryIso}/cities.json",
            optional: true,
            cancellationToken: cancellationToken);

        foreach (var model in models)
        {
            var document = new LocationCityDocument
            {
                CountryCode = model.CountryCode,
                CityCode = model.CityCode,
                Name = model.Name,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                IsCoastalCity = model.IsCoastalCity,
                IsActive = model.IsActive
            };

            await _locationRepository.UpsertCityAsync(document, cancellationToken);
        }
    }

    private async Task SeedAllDistrictsAsync(string countryIso, CancellationToken cancellationToken)
    {
        var models = await _reader.ReadAllInDirectoryAsync<LocationDistrictSeedModel>(
            $"Location/{countryIso}/Districts",
            cancellationToken);

        foreach (var model in models)
        {
            var document = new LocationDistrictDocument
            {
                CountryCode = model.CountryCode,
                CityCode = model.CityCode,
                DistrictCode = model.DistrictCode,
                Name = model.Name,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                IsCoastalDistrict = model.IsCoastalDistrict,
                IsActive = model.IsActive
            };

            await _locationRepository.UpsertDistrictAsync(document, cancellationToken);
        }
    }

    private async Task SeedAllNeighborhoodsAsync(string countryIso, CancellationToken cancellationToken)
    {
        var models = await _reader.ReadAllInDirectoryAsync<LocationNeighborhoodSeedModel>(
            $"Location/{countryIso}/Neighborhoods",
            cancellationToken);

        foreach (var model in models)
        {
            var document = new LocationNeighborhoodDocument
            {
                CountryCode = model.CountryCode,
                CityCode = model.CityCode,
                DistrictCode = model.DistrictCode,
                NeighborhoodCode = model.NeighborhoodCode,
                Name = model.Name,
                PostalCode = model.PostalCode,
                IsActive = model.IsActive
            };

            await _locationRepository.UpsertNeighborhoodAsync(document, cancellationToken);
        }
    }

    private async Task SeedAllStreetsAsync(string countryIso, CancellationToken cancellationToken)
    {
        var models = await _reader.ReadAllInDirectoryAsync<LocationStreetSeedModel>(
            $"Location/{countryIso}/Streets",
            cancellationToken);

        foreach (var model in models)
        {
            var document = new LocationStreetDocument
            {
                CountryCode = model.CountryCode,
                CityCode = model.CityCode,
                DistrictCode = model.DistrictCode,
                NeighborhoodCode = model.NeighborhoodCode,
                StreetCode = model.StreetCode,
                Name = model.Name,
                PostalCode = model.PostalCode,
                IsActive = model.IsActive
            };

            await _locationRepository.UpsertStreetAsync(document, cancellationToken);
        }
    }
}
