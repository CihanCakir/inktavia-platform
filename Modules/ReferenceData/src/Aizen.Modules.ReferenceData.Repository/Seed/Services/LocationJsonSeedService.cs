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
        // AnyCountryAsync kısa-devresi BİLEREK KALDIRILDI. Eskiden ülke yazıldıktan sonra herhangi bir adım
        // patlarsa (ör. slug index'i E11000) guard her yeniden başlatmada "ülke var" deyip erken döner, kalan
        // şehir/ilçe/mahalle/cadde bir daha ASLA denenmezdi — sistem kısmi durumda kilitlenirdi. Upsert'ler doğal
        // anahtara göre idempotenttir (ör. UpsertCityAsync CountryCode+CityCode) ve veri kümesi küçüktür; her
        // açılışta yeniden çalıştırmak ucuzdur ve kısmi başarısızlığı kendiliğinden onarır (guard yalnızca bir
        // performans optimizasyonuydu, doğruluk garantisi değil).
        // Full ISO country list (flat array) — powers the mobile flag picker's searchable list + pinned yacht-flag
        // states. Idempotent by CountryCode; runs before the TR per-folder seed (which then overlays TR's richer doc
        // and its city/district tree). Optional file: absent → no-op, TR still seeds from its folder below.
        await SeedAllCountriesAsync(cancellationToken);

        await SeedCountryAsync("TR", cancellationToken);
        await SeedCitiesAsync("TR", cancellationToken);
        await SeedAllDistrictsAsync("TR", cancellationToken);
        await SeedAllNeighborhoodsAsync("TR", cancellationToken);
        await SeedAllStreetsAsync("TR", cancellationToken);
    }

    private async Task SeedAllCountriesAsync(CancellationToken cancellationToken)
    {
        var models = await _reader.ReadListAsync<LocationCountrySeedModel>(
            "Location/countries.json",
            optional: true,
            cancellationToken: cancellationToken);

        foreach (var model in models)
        {
            if (string.IsNullOrWhiteSpace(model.CountryCode)) continue;

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
