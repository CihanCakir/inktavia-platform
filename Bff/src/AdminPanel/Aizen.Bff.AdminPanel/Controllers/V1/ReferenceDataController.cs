using Aizen.Bff.AdminPanel.Application.ReferenceData.Command;
using Aizen.Bff.AdminPanel.Application.ReferenceData.Query;
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
[Route("api/v1/admin-panel/reference-data")]
[Tags("Admin Panel - Reference Data")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class ReferenceDataController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    private readonly IReferenceDataRemoteCall _rd;

    public ReferenceDataController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs, IReferenceDataRemoteCall rd)
        : base(httpContextAccessor) { _cqrs = cqrs; _rd = rd; }

    // Lookup item UPDATE — thin passthrough to the module admin PUT (carries the new DisplayNameTr).
    // Route mirrors admin-web's URL; groupId is not needed by the module (update is keyed on item id).
    // DELTA vs admin-web payload: module takes the FLAT UpdateLookupItemRequest
    // ({ name, displayNameTr, description?, iconKey?, colorCode?, sortOrder, isDefault, isActive }); admin-web
    // currently PUTs { code, sortOrder, names:{en,tr,fr}, isEnabled }. See report — admin-web must map
    // names.en→name, names.tr→displayNameTr, isEnabled→isActive (fr unsupported: no French column).
    [HttpPut("lookup-groups/{groupId}/items/{id:long}")]
    [Tags("Admin Panel - Lookup")]
    public async Task<AizenApiResponse<LookupItemDto?>> UpdateLookupItem(
        string groupId, [FromRoute] long id, [FromBody] UpdateLookupItemRequest req)
        => SetResponse((await _rd.UpdateLookupItem(id, req))?.Body);

    [HttpGet("lookup-groups")]
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(LookupGroupListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupGroupListResult>> GetLookupGroups(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetReferenceDataLookupGroupsBffQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("lookup-tree")]
    [ProducesResponseType(typeof(LookupGroupTreeResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupGroupTreeResult>> GetLookupTree(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetReferenceDataLookupGroupTreeBffQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("lookup/{groupCode}/items")]
    [ProducesResponseType(typeof(LookupItemListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupItemListResult>> GetLookupItemsByGroupCode(
        string groupCode, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetReferenceDataLookupItemsByGroupCodeBffQuery(groupCode), ct);
        return SetResponse(result);
    }

    [HttpGet("currencies")]
    [HttpGet("currency")]
    [ProducesResponseType(typeof(CurrencyListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CurrencyListResult>> GetCurrencies(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetReferenceDataCurrenciesBffQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("locations/countries")]
    [HttpGet("location")]
    [ProducesResponseType(typeof(CountryListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CountryListResult>> GetCountries(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetReferenceDataCountriesBffQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("locations/{countryCode}/cities")]
    [ProducesResponseType(typeof(CityListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CityListResult>> GetCities(
        [FromRoute] string countryCode, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetReferenceDataCitiesBffQuery(countryCode), ct);
        return SetResponse(result);
    }

    [HttpGet("measurement-units")]
    [HttpGet("measurement")]
    [ProducesResponseType(typeof(MeasurementUnitListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MeasurementUnitListResult>> GetMeasurementUnits(
        [FromQuery] string? type, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetReferenceDataMeasurementUnitsBffQuery(type), ct);
        return SetResponse(result);
    }

    [HttpGet("system-parameters")]
    [HttpGet("system-parameter")]
    [ProducesResponseType(typeof(SystemParameterListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SystemParameterListResult>> GetSystemParameters(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetReferenceDataSystemParametersBffQuery(), ct);
        return SetResponse(result);
    }

    [HttpPost("lookup")]
    [HttpPost("lookup-groups")]
    [ProducesResponseType(typeof(LookupGroupDto), StatusCodes.Status201Created)]
    public async Task<AizenApiResponse<LookupGroupDto>> CreateLookupGroup(
        [FromBody] CreateLookupGroupRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new CreateLookupGroupBffCommand(request), ct);
        return SetResponse(result);
    }

    [HttpPost("lookup/{groupCode}/items")]
    [ProducesResponseType(typeof(LookupItemDto), StatusCodes.Status201Created)]
    public async Task<AizenApiResponse<LookupItemDto>> CreateLookupItem(
        string groupCode, [FromBody] CreateLookupItemRequest request, CancellationToken ct)
    {
        request.GroupCode = groupCode;
        var result = await _cqrs.ProcessAsync(new CreateLookupItemBffCommand(request), ct);
        return SetResponse(result);
    }
}
