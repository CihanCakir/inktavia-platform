using Aizen.Bff.Marine.Web.Application.Contracts.Location;
using Aizen.Bff.Marine.Web.Application.Location.Query.GetWebCityDetail;
using Aizen.Bff.Marine.Web.Application.Location.Query.GetWebCountryDetail;
using Aizen.Bff.Marine.Web.Application.Location.Query.GetWebDistrictDetail;
using Aizen.Bff.Marine.Web.Application.Location.Query.GetWebLocationBySlug;
using Aizen.Bff.Marine.Web.Filters;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Bff.Marine.Web.Controllers.V1;

/// <summary>
/// Public location detail for the website — anonymous, cached, trusted-caller aware. Thin: dispatches a query.
/// M3 added the collapsed <c>GET {slug}</c> resolver; the code-keyed country/city/district routes are KEPT for
/// back-compat and for coordinate-rich detail (the slug route is minimal: identity + parent chain). Region/marina
/// remain BLOCKED (a data-sourcing project) — this controller never fabricates them.
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

    // M3 — collapsed single-slug lookup (identity + parent chain). Literal-segment routes below win for their paths.
    [HttpGet("{slug}")]
    [EnableRateLimiting("public-read-ip")]
    [WebCache(WebCacheAttribute.LocationsMaxAge, WebCacheAttribute.LocationsSwr)]
    public async Task<AizenApiResponse<WebLocationDetailDto?>> BySlug(
        [FromRoute] string slug, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebLocationBySlugQuery { Slug = slug }, ct);
        return SetResponse(result);
    }

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
