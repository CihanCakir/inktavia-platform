using Aizen.Bff.Marine.Web.Application.Contracts.Location;
using Aizen.Bff.Marine.Web.Application.Location.Query.GetWebCityDetail;
using Aizen.Bff.Marine.Web.Application.Location.Query.GetWebCountryDetail;
using Aizen.Bff.Marine.Web.Application.Location.Query.GetWebDistrictDetail;
using Aizen.Bff.Marine.Web.Filters;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Bff.Marine.Web.Controllers.V1;

/// <summary>
/// Public location detail for the website (W4 priority 3, PARTIAL) — anonymous, cached, trusted-caller aware. Thin:
/// dispatches a query. Only code-keyed country/city/district are reachable in ReferenceData today; the routes mirror
/// that hierarchy. A single-slug resolver and region/marina types are NOT available and are documented as BLOCKED in
/// <c>docs/MARINE_WEB_BLOCKED.md</c> — this controller never fabricates them.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/web/locations")]
[Tags("Web - Locations")]
public sealed class LocationController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public LocationController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
        => _cqrs = cqrs;

    [HttpGet("countries/{countryCode}")]
    [EnableRateLimiting("public-read-ip")]
    [WebCache(WebCacheAttribute.LocationsMaxAge, WebCacheAttribute.LocationsSwr)]
    public async Task<AizenApiResponse<WebLocationDetailDto?>> Country(
        [FromRoute] string countryCode, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebCountryDetailQuery { CountryCode = countryCode }, ct);
        return SetResponse(result);
    }

    [HttpGet("countries/{countryCode}/cities/{cityCode}")]
    [EnableRateLimiting("public-read-ip")]
    [WebCache(WebCacheAttribute.LocationsMaxAge, WebCacheAttribute.LocationsSwr)]
    public async Task<AizenApiResponse<WebLocationDetailDto?>> City(
        [FromRoute] string countryCode, [FromRoute] string cityCode, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetWebCityDetailQuery { CountryCode = countryCode, CityCode = cityCode }, ct);
        return SetResponse(result);
    }

    [HttpGet("countries/{countryCode}/cities/{cityCode}/districts/{districtCode}")]
    [EnableRateLimiting("public-read-ip")]
    [WebCache(WebCacheAttribute.LocationsMaxAge, WebCacheAttribute.LocationsSwr)]
    public async Task<AizenApiResponse<WebLocationDetailDto?>> District(
        [FromRoute] string countryCode,
        [FromRoute] string cityCode,
        [FromRoute] string districtCode,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetWebDistrictDetailQuery { CountryCode = countryCode, CityCode = cityCode, DistrictCode = districtCode }, ct);
        return SetResponse(result);
    }
}
