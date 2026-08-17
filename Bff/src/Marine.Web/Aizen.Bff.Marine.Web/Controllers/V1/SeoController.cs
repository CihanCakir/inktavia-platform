using Aizen.Bff.Marine.Web.Application.Contracts.Seo;
using Aizen.Bff.Marine.Web.Application.Seo.Query.GetWebSeoSlugs;
using Aizen.Bff.Marine.Web.Filters;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Bff.Marine.Web.Controllers.V1;

/// <summary>
/// Public SEO surface for the server-rendered website — anonymous, cached, trusted-caller aware (same
/// `public-read-ip` policy, which routes a valid X-Aizen-Web-Caller onto the high trusted tier). Thin: dispatches a
/// query. Feeds the sitemap and static generation.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/web/seo")]
[Tags("Web - SEO")]
public sealed class SeoController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public SeoController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
        => _cqrs = cqrs;

    /// <summary>
    /// GET api/v1/web/seo/slugs/{entityType}?lang=tr → the slug feed for the sitemap. Only `content` is reachable
    /// today; other entity types are rejected (not faked). LastModified is the item's PublishedAt (see README caveat).
    /// </summary>
    [HttpGet("slugs/{entityType}")]
    [EnableRateLimiting("public-read-ip")]
    [WebCache(WebCacheAttribute.SlugFeedMaxAge, WebCacheAttribute.SlugFeedSwr)]
    public async Task<AizenApiResponse<List<WebSeoSlugItemDto>?>> Slugs(
        [FromRoute] string entityType,
        [FromQuery] string lang = "tr",
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebSeoSlugsQuery { EntityType = entityType, Lang = lang }, ct);
        return SetResponse(result);
    }
}
