using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel")]
[Tags("Admin Panel - Reference Data")]
public sealed class AdminReferenceDataController : AizenWebApiController
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;

    public AdminReferenceDataController(
        IHttpContextAccessor httpContextAccessor,
        IReferenceDataAdminBffRemoteCall referenceData)
        : base(httpContextAccessor)
    {
        _referenceData = referenceData;
    }

    [HttpGet("reference-data/lookup-groups")]
    [ProducesResponseType(typeof(LookupGroupListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupGroupListResult>> GetLookupGroups(CancellationToken ct)
    {
        var result = await _referenceData.GetLookupGroups();
        return SetResponse(result.Body);
    }

    [HttpGet("reference-data/lookup-tree")]
    [ProducesResponseType(typeof(LookupGroupTreeResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupGroupTreeResult>> GetLookupTree(CancellationToken ct)
    {
        var result = await _referenceData.GetLookupGroupTree();
        return SetResponse(result.Body);
    }

    [HttpGet("reference-data/lookup/{groupCode}/items")]
    [ProducesResponseType(typeof(LookupItemListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupItemListResult>> GetLookupItemsByGroupCode(
        string groupCode, CancellationToken ct)
    {
        var result = await _referenceData.GetLookupItemsByGroupCode(groupCode);
        return SetResponse(result.Body);
    }

    [HttpGet("reference-data/currencies")]
    [ProducesResponseType(typeof(CurrencyListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CurrencyListResult>> GetCurrencies(CancellationToken ct)
    {
        var result = await _referenceData.GetCurrencies();
        return SetResponse(result.Body);
    }

    [HttpGet("reference-data/locations/countries")]
    [ProducesResponseType(typeof(CountryListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CountryListResult>> GetCountries(CancellationToken ct)
    {
        var result = await _referenceData.GetCountries();
        return SetResponse(result.Body);
    }

    [HttpGet("reference-data/locations/cities")]
    [ProducesResponseType(typeof(CityListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CityListResult>> GetCities(
        [FromQuery] long? countryId, CancellationToken ct)
    {
        var result = await _referenceData.GetCities(countryId);
        return SetResponse(result.Body);
    }

    [HttpGet("reference-data/measurement-units")]
    [ProducesResponseType(typeof(MeasurementUnitListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MeasurementUnitListResult>> GetMeasurementUnits(
        [FromQuery] string? type, CancellationToken ct)
    {
        var result = await _referenceData.GetMeasurementUnits(type);
        return SetResponse(result.Body);
    }

    [HttpGet("reference-data/system-parameters")]
    [ProducesResponseType(typeof(SystemParameterListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SystemParameterListResult>> GetSystemParameters(CancellationToken ct)
    {
        var result = await _referenceData.GetSystemParameters();
        return SetResponse(result.Body);
    }
}
