using Aizen.Bff.Marine.Web.Application.Contracts.ServicePages;
using Aizen.Bff.Marine.Web.Application.ServicePages.Query.GetWebServicePage;
using Aizen.Bff.Marine.Web.Filters;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Bff.Marine.Web.Controllers.V1;

/// <summary>
/// Public service × location landing (M2, interim code form) — anonymous, cached, trusted-caller aware. ONE call the
/// page renders from: service summary + city summary + a COARSE availability signal (available/limited/none). The
/// full <c>{serviceSlug}/{locationSlug}</c> slug form lands with M3's location-slug resolver; until then the city is
/// supplied as a code. Carries no provider count/ids.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/web/service-pages")]
[Tags("Web - Service Pages")]
public sealed class ServicePageController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServicePageController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
        => _cqrs = cqrs;

    // M2 interim form — city supplied as a code. Kept working during the transition.
    [HttpGet("{serviceSlug}")]
    [EnableRateLimiting("public-read-ip")]
    [WebCache(WebCacheAttribute.ServicePageMaxAge, WebCacheAttribute.ServicePageSwr)]
    public async Task<AizenApiResponse<WebServicePageDto?>> ServicePage(
        [FromRoute] string serviceSlug,
        [FromQuery] string cityCode,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetWebServicePageQuery { ServiceSlug = serviceSlug, CityCode = cityCode }, ct);
        return SetResponse(result);
    }

    // M3 slug form — location supplied as a slug, resolved to its city for the (city-keyed) availability signal.
    [HttpGet("{serviceSlug}/{locationSlug}")]
    [EnableRateLimiting("public-read-ip")]
    [WebCache(WebCacheAttribute.ServicePageMaxAge, WebCacheAttribute.ServicePageSwr)]
    public async Task<AizenApiResponse<WebServicePageDto?>> ServicePageBySlug(
        [FromRoute] string serviceSlug,
        [FromRoute] string locationSlug,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetWebServicePageQuery { ServiceSlug = serviceSlug, LocationSlug = locationSlug }, ct);
        return SetResponse(result);
    }
}
