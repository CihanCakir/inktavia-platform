using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Model;
using Aizen.Modules.ReferenceData.Application.Location.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

[ApiController]
[Route("api/v1/reference-data/locations")]
[Tags("Location")]
[DocumentationInfo("Location read endpoints", "Read-only queries for countries, cities, districts and neighborhoods.")]
public sealed class LocationController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public LocationController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet("countries")]
    [ProducesResponseType(typeof(IReadOnlyList<CountryDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<CountryDto>>> GetCountries([FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<CountryDto>>(new GetCountriesQuery(onlyActive), ct);
        return SetResponse(result);
    }

    [HttpGet("countries/{countryCode}")]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CountryDto?>> GetCountry([FromRoute] string countryCode, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CountryDto?>(new GetCountryDetailQuery(countryCode), ct);
        return SetResponse(result);
    }

    [HttpGet("{countryCode}/cities")]
    [ProducesResponseType(typeof(IReadOnlyList<CityDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<CityDto>>> GetCities([FromRoute] string countryCode, [FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<CityDto>>(new GetCitiesByCountryQuery(countryCode, onlyActive), ct);
        return SetResponse(result);
    }

    [HttpGet("{countryCode}/cities/{cityCode}")]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CityDto?>> GetCity([FromRoute] string countryCode, [FromRoute] string cityCode, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CityDto?>(new GetCityDetailQuery(countryCode, cityCode), ct);
        return SetResponse(result);
    }

    [HttpGet("{countryCode}/cities/{cityCode}/districts")]
    [ProducesResponseType(typeof(IReadOnlyList<DistrictDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<DistrictDto>>> GetDistricts([FromRoute] string countryCode, [FromRoute] string cityCode, [FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<DistrictDto>>(new GetDistrictsByCityQuery(countryCode, cityCode, onlyActive), ct);
        return SetResponse(result);
    }

    [HttpGet("{countryCode}/cities/{cityCode}/districts/{districtCode}/neighborhoods")]
    [ProducesResponseType(typeof(IReadOnlyList<NeighborhoodDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<NeighborhoodDto>>> GetNeighborhoods([FromRoute] string countryCode, [FromRoute] string cityCode, [FromRoute] string districtCode, [FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<NeighborhoodDto>>(new GetNeighborhoodsByDistrictQuery(countryCode, cityCode, districtCode, onlyActive), ct);
        return SetResponse(result);
    }
}
