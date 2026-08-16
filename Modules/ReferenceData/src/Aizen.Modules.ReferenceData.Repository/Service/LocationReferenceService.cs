using Aizen.Modules.ReferenceData.Repository.Mappings;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Request.Location;
using Aizen.Modules.ReferenceData.Domain.Documents.Location;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class LocationReferenceService : ILocationReferenceService
{
    private readonly ILocationRepository _repo;

    public LocationReferenceService(ILocationRepository repo)
    {
        _repo = repo;
    }

    public async Task<CountryDto> CreateCountryAsync(CreateCountryRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repo.GetCountryAsync(request.CountryCode, cancellationToken);
        if (existing != null) throw new AizenBusinessException($"Country '{request.CountryCode}' already exists.");

        var doc = new LocationCountryDocument
        {
            CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
            NumericCode = request.NumericCode.Trim(),
            Name = request.Name,
            DefaultCurrencyCode = request.DefaultCurrencyCode.Trim().ToUpperInvariant(),
            PhoneCode = request.PhoneCode.Trim(),
            IsActive = true
        };
        await _repo.UpsertCountryAsync(doc, cancellationToken);
        return doc.ToDto();
    }

    public async Task<CountryDto> UpdateCountryAsync(string countryCode, string defaultCurrencyCode, string phoneCode, bool isActive, CancellationToken cancellationToken = default)
    {
        var doc = await _repo.GetCountryAsync(countryCode, cancellationToken)
            ?? throw new AizenBusinessException($"Country '{countryCode}' not found.");
        doc.DefaultCurrencyCode = defaultCurrencyCode.Trim().ToUpperInvariant();
        doc.PhoneCode = phoneCode.Trim();
        doc.IsActive = isActive;
        await _repo.UpsertCountryAsync(doc, cancellationToken);
        return doc.ToDto();
    }

    public async Task<IReadOnlyList<CountryDto>> GetCountriesAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetCountriesAsync(onlyActive, cancellationToken);
        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<CountryDto?> GetCountryAsync(string countryCode, CancellationToken cancellationToken = default)
    {
        var doc = await _repo.GetCountryAsync(countryCode, cancellationToken);
        return doc?.ToDto();
    }

    public async Task<CityDto> CreateCityAsync(CreateCityRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repo.GetCityAsync(request.CountryCode, request.CityCode, cancellationToken);
        if (existing != null) throw new AizenBusinessException($"City '{request.CityCode}' already exists in country '{request.CountryCode}'.");

        var doc = new LocationCityDocument
        {
            CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
            CityCode = request.CityCode.Trim().ToUpperInvariant(),
            Name = request.Name,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsCoastalCity = request.IsCoastalCity,
            IsActive = true
        };
        await _repo.UpsertCityAsync(doc, cancellationToken);
        return doc.ToDto();
    }

    public async Task<CityDto> UpdateCityAsync(string countryCode, string cityCode, decimal? latitude, decimal? longitude, bool isCoastalCity, bool isActive, CancellationToken cancellationToken = default)
    {
        var doc = await _repo.GetCityAsync(countryCode, cityCode, cancellationToken)
            ?? throw new AizenBusinessException($"City '{cityCode}' not found in country '{countryCode}'.");
        doc.Latitude = latitude;
        doc.Longitude = longitude;
        doc.IsCoastalCity = isCoastalCity;
        doc.IsActive = isActive;
        await _repo.UpsertCityAsync(doc, cancellationToken);
        return doc.ToDto();
    }

    public async Task<IReadOnlyList<CityDto>> GetCitiesByCountryAsync(string countryCode, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetCitiesByCountryAsync(countryCode, onlyActive, cancellationToken);
        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<CityDto?> GetCityAsync(string countryCode, string cityCode, CancellationToken cancellationToken = default)
    {
        var doc = await _repo.GetCityAsync(countryCode, cityCode, cancellationToken);
        return doc?.ToDto();
    }

    public async Task<DistrictDto> CreateDistrictAsync(string countryCode, string cityCode, string districtCode, Dictionary<string, string> name, decimal? latitude, decimal? longitude, bool isCoastalDistrict, CancellationToken cancellationToken = default)
    {
        var existing = await _repo.GetDistrictAsync(countryCode, cityCode, districtCode, cancellationToken);
        if (existing != null) throw new AizenBusinessException($"District '{districtCode}' already exists.");

        var doc = new LocationDistrictDocument
        {
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            CityCode = cityCode.Trim().ToUpperInvariant(),
            DistrictCode = districtCode.Trim().ToUpperInvariant(),
            Name = name,
            Latitude = latitude,
            Longitude = longitude,
            IsCoastalDistrict = isCoastalDistrict,
            IsActive = true
        };
        await _repo.UpsertDistrictAsync(doc, cancellationToken);
        return doc.ToDto();
    }

    public async Task<DistrictDto> UpdateDistrictAsync(string countryCode, string cityCode, string districtCode, decimal? latitude, decimal? longitude, bool isCoastalDistrict, bool isActive, CancellationToken cancellationToken = default)
    {
        var doc = await _repo.GetDistrictAsync(countryCode, cityCode, districtCode, cancellationToken)
            ?? throw new AizenBusinessException($"District '{districtCode}' not found.");
        doc.Latitude = latitude;
        doc.Longitude = longitude;
        doc.IsCoastalDistrict = isCoastalDistrict;
        doc.IsActive = isActive;
        await _repo.UpsertDistrictAsync(doc, cancellationToken);
        return doc.ToDto();
    }

    public async Task<IReadOnlyList<DistrictDto>> GetDistrictsByCityAsync(string countryCode, string cityCode, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetDistrictsByCityAsync(countryCode, cityCode, onlyActive, cancellationToken);
        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<NeighborhoodDto> CreateNeighborhoodAsync(string countryCode, string cityCode, string districtCode, string neighborhoodCode, Dictionary<string, string> name, string? postalCode, CancellationToken cancellationToken = default)
    {
        var existing = await _repo.GetNeighborhoodsByDistrictAsync(countryCode, cityCode, districtCode, false, cancellationToken);
        if (existing.Any(x => x.NeighborhoodCode == neighborhoodCode.Trim().ToUpperInvariant()))
            throw new AizenBusinessException($"Neighborhood '{neighborhoodCode}' already exists.");

        var doc = new LocationNeighborhoodDocument
        {
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            CityCode = cityCode.Trim().ToUpperInvariant(),
            DistrictCode = districtCode.Trim().ToUpperInvariant(),
            NeighborhoodCode = neighborhoodCode.Trim().ToUpperInvariant(),
            Name = name,
            PostalCode = postalCode,
            IsActive = true
        };
        await _repo.UpsertNeighborhoodAsync(doc, cancellationToken);
        return doc.ToDto();
    }

    public async Task<NeighborhoodDto> UpdateNeighborhoodAsync(string countryCode, string cityCode, string districtCode, string neighborhoodCode, string? postalCode, bool isActive, CancellationToken cancellationToken = default)
    {
        var normalizedCode = neighborhoodCode.Trim().ToUpperInvariant();
        var list = await _repo.GetNeighborhoodsByDistrictAsync(countryCode, cityCode, districtCode, false, cancellationToken);
        var doc = list.FirstOrDefault(x => x.NeighborhoodCode == normalizedCode)
            ?? throw new AizenBusinessException($"Neighborhood '{neighborhoodCode}' not found.");
        doc.PostalCode = postalCode;
        doc.IsActive = isActive;
        await _repo.UpsertNeighborhoodAsync(doc, cancellationToken);
        return doc.ToDto();
    }

    public async Task<IReadOnlyList<NeighborhoodDto>> GetNeighborhoodsByDistrictAsync(string countryCode, string cityCode, string districtCode, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetNeighborhoodsByDistrictAsync(countryCode, cityCode, districtCode, onlyActive, cancellationToken);
        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<StreetDto> CreateStreetAsync(string countryCode, string cityCode, string districtCode, string? neighborhoodCode, string streetCode, Dictionary<string, string> name, string? postalCode, CancellationToken cancellationToken = default)
    {
        var doc = new LocationStreetDocument
        {
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            CityCode = cityCode.Trim().ToUpperInvariant(),
            DistrictCode = districtCode.Trim().ToUpperInvariant(),
            NeighborhoodCode = neighborhoodCode?.Trim().ToUpperInvariant(),
            StreetCode = streetCode.Trim().ToUpperInvariant(),
            Name = name,
            PostalCode = postalCode,
            IsActive = true
        };
        await _repo.UpsertStreetAsync(doc, cancellationToken);
        return doc.ToDto();
    }

    public async Task<IReadOnlyList<StreetDto>> GetStreetsByNeighborhoodAsync(string countryCode, string cityCode, string districtCode, string? neighborhoodCode, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetStreetsByNeighborhoodAsync(countryCode, cityCode, districtCode, neighborhoodCode, onlyActive, cancellationToken);
        return list.Select(x => x.ToDto()).ToList();
    }

    // ── M3 single reads + by-slug resolver ─────────────────────────────────────

    public async Task<DistrictDto?> GetDistrictAsync(string countryCode, string cityCode, string districtCode, CancellationToken cancellationToken = default)
        => (await _repo.GetDistrictAsync(countryCode, cityCode, districtCode, cancellationToken))?.ToDto();

    public async Task<NeighborhoodDto?> GetNeighborhoodAsync(string countryCode, string cityCode, string districtCode, string neighborhoodCode, CancellationToken cancellationToken = default)
        => (await _repo.GetNeighborhoodAsync(countryCode, cityCode, districtCode, neighborhoodCode, cancellationToken))?.ToDto();

    public async Task<LocationBySlugDto?> ResolveBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var country = await _repo.GetCountryBySlugAsync(slug, cancellationToken);
        if (country is not null)
            return new LocationBySlugDto
            {
                LocationType = "country", Code = country.CountryCode, Slug = country.Slug!,
                Name = Display(country.Name, country.CountryCode), ParentChain = new(),
            };

        var city = await _repo.GetCityBySlugAsync(slug, cancellationToken);
        if (city is not null)
        {
            var co = await _repo.GetCountryAsync(city.CountryCode, cancellationToken);
            return new LocationBySlugDto
            {
                LocationType = "city", Code = city.CityCode, Slug = city.Slug!,
                Name = Display(city.Name, city.CityCode),
                ParentChain = new() { Ref("country", city.CountryCode, co?.Name) },
            };
        }

        var district = await _repo.GetDistrictBySlugAsync(slug, cancellationToken);
        if (district is not null)
        {
            var co = await _repo.GetCountryAsync(district.CountryCode, cancellationToken);
            var ci = await _repo.GetCityAsync(district.CountryCode, district.CityCode, cancellationToken);
            return new LocationBySlugDto
            {
                LocationType = "district", Code = district.DistrictCode, Slug = district.Slug!,
                Name = Display(district.Name, district.DistrictCode),
                ParentChain = new()
                {
                    Ref("country", district.CountryCode, co?.Name),
                    Ref("city", district.CityCode, ci?.Name),
                },
            };
        }

        var nb = await _repo.GetNeighborhoodBySlugAsync(slug, cancellationToken);
        if (nb is not null)
        {
            var co = await _repo.GetCountryAsync(nb.CountryCode, cancellationToken);
            var ci = await _repo.GetCityAsync(nb.CountryCode, nb.CityCode, cancellationToken);
            var di = await _repo.GetDistrictAsync(nb.CountryCode, nb.CityCode, nb.DistrictCode, cancellationToken);
            return new LocationBySlugDto
            {
                LocationType = "neighborhood", Code = nb.NeighborhoodCode, Slug = nb.Slug!,
                Name = Display(nb.Name, nb.NeighborhoodCode),
                ParentChain = new()
                {
                    Ref("country", nb.CountryCode, co?.Name),
                    Ref("city", nb.CityCode, ci?.Name),
                    Ref("district", nb.DistrictCode, di?.Name),
                },
            };
        }

        return null;
    }

    private static LocationRefDto Ref(string type, string code, IReadOnlyDictionary<string, string>? name)
        => new() { LocationType = type, Code = code, Name = name is null ? code : Display(name, code) };

    private static string Display(IReadOnlyDictionary<string, string> name, string fallbackCode)
        => name.GetValueOrDefault("en") ?? name.Values.FirstOrDefault() ?? fallbackCode;
}
