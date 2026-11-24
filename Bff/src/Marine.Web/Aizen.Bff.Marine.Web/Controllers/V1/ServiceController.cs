using Aizen.Bff.Marine.Web.Application.Catalogue.Query.GetWebServiceCatalogue;
using Aizen.Bff.Marine.Web.Application.Contracts.Catalogue;
using Aizen.Bff.Marine.Web.Filters;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Bff.Marine.Web.Controllers.V1;

/// <summary>
/// Public service catalogue for the website (W4 priority 1) — anonymous, cached, trusted-caller aware. Thin:
/// dispatches a query. The taxonomy comes from the ReferenceData <c>SERVICE_PROVIDER_CATEGORY</c> lookup group; a
/// per-service rich detail page is NOT served here (it is editorial content under <c>api/v1/web/content</c>).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/web/services")]
[Tags("Web - Services")]
public sealed class ServiceController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ServiceController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
        => _cqrs = cqrs;

    /// <summary>
    /// <paramref name="lang"/> selects the display language and the canonical <c>slug</c>; every tile carries the
    /// full <c>slugs</c> map either way. It is a QUERY parameter, not a header, on purpose: the response is cached
    /// with an ETag and consumed by a server-rendered client, and a URL that varies is a cache key that varies —
    /// with a header we would need <c>Vary</c> and would still be at the mercy of intermediaries that ignore it.
    /// </summary>
    [HttpGet]
    [EnableRateLimiting("public-read-ip")]
    [WebCache(WebCacheAttribute.ServicesMaxAge, WebCacheAttribute.ServicesSwr)]
    public async Task<AizenApiResponse<List<WebServiceSummaryDto>?>> Catalogue(
        [FromQuery] string? lang = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebServiceCatalogueQuery { Lang = lang }, ct);
        return SetResponse(result);
    }
}
