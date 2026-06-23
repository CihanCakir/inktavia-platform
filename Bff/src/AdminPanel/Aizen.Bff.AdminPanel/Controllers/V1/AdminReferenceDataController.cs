using Aizen.Bff.AdminPanel.Application.AdminReferenceData.Command;
using Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupItem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Reference Data")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class ReferenceDataController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ReferenceDataController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor)
    {
        _cqrs = cqrsProcessor;
    }

    [HttpGet("reference-data/lookup-groups")]
    [HttpGet("reference-data/lookup")]
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
    [HttpGet("reference-data/currency")]
    [ProducesResponseType(typeof(CurrencyListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CurrencyListResult>> GetCurrencies(CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetReferenceDataCurrenciesQuery(userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("reference-data/locations/countries")]
    [HttpGet("reference-data/location")]
    [ProducesResponseType(typeof(CountryListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CountryListResult>> GetCountries(CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetReferenceDataCountriesQuery(userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("reference-data/locations/{countryCode}/cities")]
    [ProducesResponseType(typeof(CityListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CityListResult>> GetCities(
        [FromRoute] string countryCode, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetReferenceDataCitiesQuery(countryCode, userToken), ct);
        return SetResponse(result);
    }

    [HttpGet("reference-data/measurement-units")]
    [HttpGet("reference-data/measurement")]
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
    [HttpGet("reference-data/system-parameter")]
    [ProducesResponseType(typeof(SystemParameterListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SystemParameterListResult>> GetSystemParameters(CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new GetReferenceDataSystemParametersQuery(userToken), ct);
        return SetResponse(result);
    }

    [HttpPost("reference-data/lookup")]
    [HttpPost("reference-data/lookup-groups")]
    [ProducesResponseType(typeof(LookupGroupDto), StatusCodes.Status201Created)]
    public async Task<AizenApiResponse<LookupGroupDto>> CreateLookupGroup(
        [FromBody] CreateLookupGroupRequest request, CancellationToken ct)
    {
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new CreateLookupGroupCommand(request, userToken), ct);
        return SetResponse(result);
    }

    [HttpPost("reference-data/lookup/{groupCode}/items")]
    [ProducesResponseType(typeof(LookupItemDto), StatusCodes.Status201Created)]
    public async Task<AizenApiResponse<LookupItemDto>> CreateLookupItem(
        string groupCode, [FromBody] CreateLookupItemRequest request, CancellationToken ct)
    {
        request.GroupCode = groupCode;
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(new CreateLookupItemCommand(request, userToken), ct);
        return SetResponse(result);
    }
}
