using Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentByType;
using Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentBySlug;
using Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentCategories;
using Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentComments;
using Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentFeed;
using Aizen.Bff.Marine.Web.Application.Contracts.Content;
using Aizen.Bff.Marine.Web.Filters;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Bff.Marine.Web.Controllers.V1;

/// <summary>
/// Public MarineOS website content surface — anonymous, IP rate-limited. Thin: every action dispatches a query.
/// The surface (MarineOsWeb) is pinned by the BFF; the caller supplies only lang/paging/type.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/web/content")]
[Tags("Web - Content")]
public sealed class ContentController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ContentController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
        => _cqrs = cqrs;

    [HttpGet("feed")]
    [EnableRateLimiting("public-read-ip")]
    [WebCache(WebCacheAttribute.FeedMaxAge, WebCacheAttribute.FeedSwr)]
    public async Task<AizenApiResponse<ContentFeedResponse?>> Feed(
        [FromQuery] string lang = "tr",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebContentFeedQuery { Lang = lang, Page = page, PageSize = pageSize }, ct);
        return SetResponse(result);
    }

    [HttpGet("by-type/{type}")]
    [EnableRateLimiting("public-read-ip")]
    [WebCache(WebCacheAttribute.FeedMaxAge, WebCacheAttribute.FeedSwr)]
    public async Task<AizenApiResponse<ContentFeedResponse?>> ByType(
        [FromRoute] ContentType type,
        [FromQuery] string lang = "tr",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebContentByTypeQuery { Type = type, Lang = lang, Page = page, PageSize = pageSize }, ct);
        return SetResponse(result);
    }

    [HttpGet("items/{slug}")]
    [EnableRateLimiting("public-read-ip")]
    [WebCache(WebCacheAttribute.DetailMaxAge, WebCacheAttribute.DetailSwr)]
    public async Task<AizenApiResponse<WebContentDetailDto?>> BySlug(
        string slug, [FromQuery] string lang = "tr", CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebContentBySlugQuery { Slug = slug, Lang = lang }, ct);
        return SetResponse(result);
    }

    [HttpGet("categories")]
    [EnableRateLimiting("public-read-ip")]
    [WebCache(WebCacheAttribute.CategoriesMaxAge, WebCacheAttribute.CategoriesSwr)]
    public async Task<AizenApiResponse<List<ContentCategoryDto>?>> Categories(
        [FromQuery] string lang = "tr", CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebContentCategoriesQuery { Lang = lang }, ct);
        return SetResponse(result);
    }

    [HttpGet("items/{id}/comments")]
    [EnableRateLimiting("public-read-ip")]
    [WebCache(WebCacheAttribute.CommentsMaxAge, WebCacheAttribute.CommentsSwr)]
    public async Task<AizenApiResponse<WebContentCommentsResponse?>> Comments(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetWebContentCommentsQuery { ContentId = id, Page = page, PageSize = pageSize }, ct);
        return SetResponse(result);
    }
}
