using Aizen.Bff.Marine.Web.Application.Reference.Query.GetWebCitiesByCountry;
using Aizen.Bff.Marine.Web.Application.Reference.Query.GetWebCountries;
using Aizen.Bff.Marine.Web.Application.Reference.Query.GetWebLookupItems;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Bff.Marine.Web.Controllers.V1;

/// <summary>
/// Public MarineOS website reference data — anonymous, IP rate-limited. Thin: every action dispatches a query.
/// Countries and cities are public on the module; lookup-items are served with the BFF service token
/// (reference_data_read role). The module DTOs are clean lookup data and pass through unchanged.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/web/reference")]
[Tags("Web - Reference")]
public sealed class ReferenceController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ReferenceController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
        => _cqrs = cqrs;

    [HttpGet("countries")]
    [EnableRateLimiting("public-read-ip")]
    public async Task<AizenApiResponse<List<CountryDto>?>> Countries(CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebCountriesQuery(), ct);
        return SetResponse(result);
    }

    [HttpGet("countries/{countryCode}/cities")]
    [EnableRateLimiting("public-read-ip")]
    public async Task<AizenApiResponse<List<CityDto>?>> Cities(
        [FromRoute] string countryCode, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebCitiesByCountryQuery { CountryCode = countryCode }, ct);
        return SetResponse(result);
    }

    [HttpGet("lookups/{groupCode}")]
    [EnableRateLimiting("public-read-ip")]
    public async Task<AizenApiResponse<List<LookupItemDto>?>> Lookups(
        [FromRoute] string groupCode, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebLookupItemsQuery { GroupCode = groupCode }, ct);
        return SetResponse(result);
    }
}
