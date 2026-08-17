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

    [HttpGet]
    [EnableRateLimiting("public-read-ip")]
    [WebCache(WebCacheAttribute.ServicesMaxAge, WebCacheAttribute.ServicesSwr)]
    public async Task<AizenApiResponse<List<WebServiceSummaryDto>?>> Catalogue(CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebServiceCatalogueQuery(), ct);
        return SetResponse(result);
    }
}
