using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Model;
using Aizen.Modules.ReferenceData.Abstraction.Request.Location;
using Aizen.Modules.ReferenceData.Application.Location.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.Admin;

[ApiController]
[Route("api/v1/admin/reference-data/locations")]
[Tags("Admin - Location")]
[Authorize(Roles = "Admin")]
[DocumentationInfo("Location admin endpoints", "Create and update countries, cities, districts, neighborhoods and streets. Requires Admin role.")]
public sealed class LocationAdminController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public LocationAdminController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    // ===== Countries =====

    [HttpPost("countries")]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CountryDto?>> CreateCountry([FromBody] CreateCountryRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CountryDto>(new CreateCountryCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("countries/{countryCode}")]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CountryDto?>> UpdateCountry([FromRoute] string countryCode, [FromBody] UpdateCountryRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CountryDto>(new UpdateCountryCommand(countryCode, req.DefaultCurrencyCode, req.PhoneCode, req.IsActive), ct);
        return SetResponse(result);
    }

    // ===== Cities =====

    [HttpPost("cities")]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CityDto?>> CreateCity([FromBody] CreateCityRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CityDto>(new CreateCityCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("{countryCode}/cities/{cityCode}")]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CityDto?>> UpdateCity([FromRoute] string countryCode, [FromRoute] string cityCode, [FromBody] UpdateCityRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<CityDto>(new UpdateCityCommand(countryCode, cityCode, req.Latitude, req.Longitude, req.IsCoastalCity, req.IsActive), ct);
        return SetResponse(result);
    }

    // ===== Districts =====

    [HttpPost("districts")]
    [ProducesResponseType(typeof(DistrictDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<DistrictDto?>> CreateDistrict([FromBody] CreateDistrictRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<DistrictDto>(new CreateDistrictCommand(req.CountryCode, req.CityCode, req.DistrictCode, req.Name, req.Latitude, req.Longitude, req.IsCoastalDistrict), ct);
        return SetResponse(result);
    }

    [HttpPut("{countryCode}/cities/{cityCode}/districts/{districtCode}")]
    [ProducesResponseType(typeof(DistrictDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<DistrictDto?>> UpdateDistrict([FromRoute] string countryCode, [FromRoute] string cityCode, [FromRoute] string districtCode, [FromBody] UpdateDistrictRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<DistrictDto>(new UpdateDistrictCommand(countryCode, cityCode, districtCode, req.Latitude, req.Longitude, req.IsCoastalDistrict, req.IsActive), ct);
        return SetResponse(result);
    }

    // ===== Neighborhoods =====

    [HttpPost("neighborhoods")]
    [ProducesResponseType(typeof(NeighborhoodDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NeighborhoodDto?>> CreateNeighborhood([FromBody] CreateNeighborhoodRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<NeighborhoodDto>(new CreateNeighborhoodCommand(req.CountryCode, req.CityCode, req.DistrictCode, req.NeighborhoodCode, req.Name, req.PostalCode), ct);
        return SetResponse(result);
    }

    [HttpPut("{countryCode}/cities/{cityCode}/districts/{districtCode}/neighborhoods/{neighborhoodCode}")]
    [ProducesResponseType(typeof(NeighborhoodDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<NeighborhoodDto?>> UpdateNeighborhood([FromRoute] string countryCode, [FromRoute] string cityCode, [FromRoute] string districtCode, [FromRoute] string neighborhoodCode, [FromBody] UpdateNeighborhoodRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<NeighborhoodDto>(new UpdateNeighborhoodCommand(countryCode, cityCode, districtCode, neighborhoodCode, req.PostalCode, req.IsActive), ct);
        return SetResponse(result);
    }

    // ===== Streets =====

    [HttpPost("streets")]
    [ProducesResponseType(typeof(StreetDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<StreetDto?>> CreateStreet([FromBody] CreateStreetRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<StreetDto>(new CreateStreetCommand(req.CountryCode, req.CityCode, req.DistrictCode, req.NeighborhoodCode, req.StreetCode, req.Name, req.PostalCode), ct);
        return SetResponse(result);
    }
}
