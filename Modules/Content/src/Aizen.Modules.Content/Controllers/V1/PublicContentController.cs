using Aizen.Core.CQRS.Abstraction;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Queries.GetContentComments;
using Aizen.Modules.Content.Application.Queries.GetPublicCategoryTree;
using Aizen.Modules.Content.Application.Queries.GetPublicContentByType;
using Aizen.Modules.Content.Application.Queries.GetPublicContentBySlug;
using Aizen.Modules.Content.Application.Queries.GetPublicContentFeed;
using Aizen.Modules.Content.Abstraction.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Modules.Content.Controllers.V1;

/// <summary>
/// Anonymous public read surface for Content (§4.6). Plain ControllerBase, [AllowAnonymous],
/// IP rate-limited. Serves only Published, in-window, Public-audience items for the requested surface.
/// Thin — every endpoint dispatches a query.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/content/public")]
[Tags("Content - Public")]
public sealed class PublicContentController : ControllerBase
{
    private readonly IAizenCQRSProcessor _cqrs;

    public PublicContentController(IAizenCQRSProcessor cqrs) => _cqrs = cqrs;

    [HttpGet("feed")]
    [EnableRateLimiting("public-read-ip")]
    public async Task<IActionResult> Feed(
        [FromQuery] ContentSurface surface,
        [FromQuery] string lang = "tr",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentFeedResponse>(new GetPublicContentFeedQuery
        {
            Surface = surface, Lang = lang, Page = page, PageSize = pageSize,
        }, ct);
        return Ok(result);
    }

    [HttpGet("by-type")]
    [EnableRateLimiting("public-read-ip")]
    public async Task<IActionResult> ByType(
        [FromQuery] ContentSurface surface,
        [FromQuery] ContentType type,
        [FromQuery] string lang = "tr",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentFeedResponse>(new GetPublicContentByTypeQuery
        {
            Surface = surface, Type = type, Lang = lang, Page = page, PageSize = pageSize,
        }, ct);
        return Ok(result);
    }

    [HttpGet("items/{slug}")]
    [EnableRateLimiting("public-read-ip")]
    public async Task<IActionResult> BySlug(
        string slug, [FromQuery] string lang = "tr", CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemDto?>(new GetPublicContentBySlugQuery
        {
            Slug = slug, Lang = lang,
        }, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("categories")]
    [EnableRateLimiting("public-read-ip")]
    public async Task<IActionResult> Categories([FromQuery] string lang = "tr", CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<List<ContentCategoryDto>>(new GetPublicCategoryTreeQuery
        {
            Lang = lang,
        }, ct);
        return Ok(result);
    }

    // Approved comments are public — they live here (not on /me) so anonymous readers see them.
    // {contentId} is the content item id (from feed/detail DTOs), not the slug.
    [HttpGet("items/{contentId}/comments")]
    [EnableRateLimiting("public-read-ip")]
    public async Task<IActionResult> Comments(
        string contentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentCommentsResponse>(new GetContentCommentsQuery
        {
            ContentId = contentId, Page = page, PageSize = pageSize,
        }, ct);
        return Ok(result);
    }
}
