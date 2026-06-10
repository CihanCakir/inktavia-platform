using Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Reference Data")]
[Authorize]
public sealed class ReferenceDataController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ReferenceDataController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor)
    {
        _cqrs = cqrsProcessor;
    }

    [HttpGet("reference-data/lookup-groups")]
    [ProducesResponseType(typeof(LookupGroupListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupGroupListResult>> GetLookupGroups(CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetReferenceDataLookupGroupsQuery(userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("reference-data/lookup-tree")]
    [ProducesResponseType(typeof(LookupGroupTreeResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupGroupTreeResult>> GetLookupTree(CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetReferenceDataLookupGroupTreeQuery(userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("reference-data/lookup/{groupCode}/items")]
    [ProducesResponseType(typeof(LookupItemListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupItemListResult>> GetLookupItemsByGroupCode(
        string groupCode, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetReferenceDataLookupItemsByGroupCodeQuery(groupCode, userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("reference-data/currencies")]
    [ProducesResponseType(typeof(CurrencyListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CurrencyListResult>> GetCurrencies(CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetReferenceDataCurrenciesQuery(userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("reference-data/locations/countries")]
    [ProducesResponseType(typeof(CountryListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CountryListResult>> GetCountries(CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetReferenceDataCountriesQuery(userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("reference-data/locations/cities")]
    [ProducesResponseType(typeof(CityListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CityListResult>> GetCities(
        [FromQuery] long? countryId, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetReferenceDataCitiesQuery(countryId, userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("reference-data/measurement-units")]
    [ProducesResponseType(typeof(MeasurementUnitListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MeasurementUnitListResult>> GetMeasurementUnits(
        [FromQuery] string? type, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetReferenceDataMeasurementUnitsQuery(type, userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("reference-data/system-parameters")]
    [ProducesResponseType(typeof(SystemParameterListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SystemParameterListResult>> GetSystemParameters(CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetReferenceDataSystemParametersQuery(userToken), ct);
        return SetResponse(result);
    }
}
