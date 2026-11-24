using Aizen.Bff.AdminPanel.Application.Content.Command;
using Aizen.Bff.AdminPanel.Application.Content.Query;
using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Abstraction.Model;
using Aizen.Modules.Content.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

/// <summary>
/// Admin surface for the Content (blog/editorial) module. Thin — every action dispatches a CQRS
/// query/command that proxies the Content module through <see cref="Application.Common.RemoteClients.IContentRemoteCall"/>.
///
/// Authorization mirrors the module's own role sets (never broader): authoring actions require
/// <see cref="ContentRoles.AdminAuthoring"/>; comment-moderation actions require
/// <see cref="ContentRoles.Moderation"/>. The module further narrows elevated actions
/// (publish/unpublish/archive/delete/categories) to Admin/SuperAdmin/ContentAdmin *inside its handlers* —
/// the BFF does not attempt to re-encode that in-handler narrowing (it would be guesswork and could drift);
/// a ContentEditor passes the BFF gate and is rejected by the module, exactly as a direct module call.
/// </summary>
[ApiController]
[Route("api/v1/admin-panel/content")]
[Tags("Admin Panel - Content")]
[Authorize]
public sealed class ContentController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ContentController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    // ── Item reads ────────────────────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/content/items — paged list (all statuses). Filters: type/status/surface/tag/lang.</summary>
    [HttpGet("items")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(ContentFeedBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentFeedBffResult>> GetItems(
        [FromQuery] ContentType? type,
        [FromQuery] ContentStatus? status,
        [FromQuery] ContentSurface? surface,
        [FromQuery] string? tag,
        [FromQuery] string lang = "tr",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetContentItemsBffQuery
        {
            Type = type, Status = status, Surface = surface, Tag = tag,
            Lang = lang, Page = page, PageSize = pageSize,
        }, ct);
        return SetResponse(result);
    }

    /// <summary>GET api/v1/admin-panel/content/items/{id} — full authoring detail incl. all translations.</summary>
    [HttpGet("items/{id}")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(ContentItemBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentItemBffDto>> GetItem(string id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemBffDto>(new GetContentItemBffQuery { Id = id }, ct);
        return SetResponse(result);
    }

    // ── Item authoring ─────────────────────────────────────────────────────────

    /// <summary>POST api/v1/admin-panel/content/items — create draft.</summary>
    [HttpPost("items")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(ContentItemBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentItemBffDto>> CreateItem(
        [FromBody] CreateContentItemRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemBffDto>(
            new CreateContentItemBffCommand { Body = body }, ct);
        return SetResponse(result);
    }

    /// <summary>PUT api/v1/admin-panel/content/items/{id} — update core editorial fields.</summary>
    [HttpPut("items/{id}")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(ContentItemBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentItemBffDto>> UpdateItem(
        string id, [FromBody] UpdateContentItemRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemBffDto>(
            new UpdateContentItemBffCommand { Id = id, Body = body }, ct);
        return SetResponse(result);
    }

    /// <summary>PUT api/v1/admin-panel/content/items/{id}/translations — add/replace a single-language translation.</summary>
    [HttpPut("items/{id}/translations")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(ContentItemBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentItemBffDto>> UpsertTranslation(
        string id, [FromBody] UpsertContentTranslationRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemBffDto>(
            new UpsertContentTranslationBffCommand { Id = id, Body = body }, ct);
        return SetResponse(result);
    }

    /// <summary>PUT api/v1/admin-panel/content/items/{id}/media — replace cover + gallery.</summary>
    [HttpPut("items/{id}/media")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(ContentItemBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentItemBffDto>> SetMedia(
        string id, [FromBody] AttachContentMediaRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemBffDto>(
            new SetContentMediaBffCommand { Id = id, Body = body }, ct);
        return SetResponse(result);
    }

    /// <summary>PUT api/v1/admin-panel/content/items/{id}/placements — replace where the item surfaces.</summary>
    [HttpPut("items/{id}/placements")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(ContentItemBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentItemBffDto>> SetPlacements(
        string id, [FromBody] SetContentPlacementsRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemBffDto>(
            new SetContentPlacementsBffCommand { Id = id, Body = body }, ct);
        return SetResponse(result);
    }

    /// <summary>PUT api/v1/admin-panel/content/items/{id}/audience — replace targeting.</summary>
    [HttpPut("items/{id}/audience")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(ContentItemBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentItemBffDto>> SetAudience(
        string id, [FromBody] SetContentAudienceRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemBffDto>(
            new SetContentAudienceBffCommand { Id = id, Body = body }, ct);
        return SetResponse(result);
    }

    // ── Lifecycle ───────────────────────────────────────────────────────────────

    /// <summary>POST api/v1/admin-panel/content/items/{id}/schedule — schedule publish.</summary>
    [HttpPost("items/{id}/schedule")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(ContentItemBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentItemBffDto>> Schedule(
        string id, [FromBody] ScheduleContentItemRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemBffDto>(
            new ScheduleContentItemBffCommand { Id = id, Body = body }, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/content/items/{id}/publish — publish now.</summary>
    [HttpPost("items/{id}/publish")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(ContentItemBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentItemBffDto>> Publish(string id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemBffDto>(
            new PublishContentItemBffCommand { Id = id }, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/content/items/{id}/unpublish — unpublish.</summary>
    [HttpPost("items/{id}/unpublish")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(ContentItemBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentItemBffDto>> Unpublish(string id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemBffDto>(
            new UnpublishContentItemBffCommand { Id = id }, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/content/items/{id}/archive — archive.</summary>
    [HttpPost("items/{id}/archive")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(ContentItemBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentItemBffDto>> Archive(string id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemBffDto>(
            new ArchiveContentItemBffCommand { Id = id }, ct);
        return SetResponse(result);
    }

    /// <summary>DELETE api/v1/admin-panel/content/items/{id} — delete.</summary>
    [HttpDelete("items/{id}")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(ContentItemBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentItemBffDto>> Delete(string id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemBffDto>(
            new DeleteContentItemBffCommand { Id = id }, ct);
        return SetResponse(result);
    }

    // ── Categories (taxonomy) ─────────────────────────────────────────────────

    /// <summary>GET api/v1/admin-panel/content/categories — taxonomy list (reads the public category tree; module has no admin list).</summary>
    [HttpGet("categories")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(List<ContentCategoryBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<ContentCategoryBffDto>>> GetCategories(
        [FromQuery] string lang = "tr", CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetContentCategoriesBffQuery { Lang = lang }, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/content/categories — create category.</summary>
    [HttpPost("categories")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(ContentCategoryBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentCategoryBffDto>> CreateCategory(
        [FromBody] CreateContentCategoryRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentCategoryBffDto>(
            new CreateContentCategoryBffCommand { Body = body }, ct);
        return SetResponse(result);
    }

    /// <summary>PUT api/v1/admin-panel/content/categories/{slug} — update category.</summary>
    [HttpPut("categories/{slug}")]
    [Authorize(Roles = ContentRoles.AdminAuthoring)]
    [ProducesResponseType(typeof(ContentCategoryBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentCategoryBffDto>> UpdateCategory(
        string slug, [FromBody] UpdateContentCategoryRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentCategoryBffDto>(
            new UpdateContentCategoryBffCommand { Slug = slug, Body = body }, ct);
        return SetResponse(result);
    }

    // ── Comment moderation ────────────────────────────────────────────────────

    /// <summary>
    /// GET api/v1/admin-panel/content/comments — moderation queue for ONE content item (contentId is required;
    /// the module has no global cross-item queue). Optional status narrows the queue (e.g. Pending).
    /// </summary>
    [HttpGet("comments")]
    [Authorize(Roles = ContentRoles.Moderation)]
    [ProducesResponseType(typeof(ContentCommentsBffResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentCommentsBffResult>> GetComments(
        [FromQuery] string contentId,
        [FromQuery] ContentCommentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(new GetContentCommentsBffQuery
        {
            ContentId = contentId, Status = status, Page = page, PageSize = pageSize,
        }, ct);
        return SetResponse(result);
    }

    /// <summary>POST api/v1/admin-panel/content/comments/{id}/moderate — approve/reject/hide.</summary>
    [HttpPost("comments/{id}/moderate")]
    [Authorize(Roles = ContentRoles.Moderation)]
    [ProducesResponseType(typeof(ContentCommentBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentCommentBffDto>> ModerateComment(
        string id, [FromBody] ModerateContentCommentRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentCommentBffDto>(
            new ModerateContentCommentBffCommand { CommentId = id, Body = body }, ct);
        return SetResponse(result);
    }

    /// <summary>DELETE api/v1/admin-panel/content/comments/{id} — delete a comment.</summary>
    [HttpDelete("comments/{id}")]
    [Authorize(Roles = ContentRoles.Moderation)]
    [ProducesResponseType(typeof(ContentCommentBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ContentCommentBffDto>> DeleteComment(string id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentCommentBffDto>(
            new DeleteContentCommentBffCommand { CommentId = id }, ct);
        return SetResponse(result);
    }
}
