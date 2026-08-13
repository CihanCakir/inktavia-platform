using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Model;
using Aizen.Modules.Content.Application.Commands.ArchiveContentItem;
using Aizen.Modules.Content.Application.Commands.AttachContentMedia;
using Aizen.Modules.Content.Application.Commands.CreateContentCategory;
using Aizen.Modules.Content.Application.Commands.CreateContentItem;
using Aizen.Modules.Content.Application.Commands.DeleteContentItem;
using Aizen.Modules.Content.Application.Commands.PublishContentItem;
using Aizen.Modules.Content.Application.Commands.ScheduleContentItem;
using Aizen.Modules.Content.Application.Commands.SetContentAudience;
using Aizen.Modules.Content.Application.Commands.SetContentPlacements;
using Aizen.Modules.Content.Application.Commands.UnpublishContentItem;
using Aizen.Modules.Content.Application.Commands.UpdateContentCategory;
using Aizen.Modules.Content.Application.Commands.UpdateContentItem;
using Aizen.Modules.Content.Application.Commands.UpsertContentTranslation;
using Aizen.Modules.Content.Application.Queries.GetAdminContentList;
using Aizen.Modules.Content.Application.Queries.GetContentByIdAdmin;
using Aizen.Modules.Content.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Content.Controllers.V1;

/// <summary>
/// Admin authoring surface for Content (§7). Broadly gated to the authoring roles; elevated actions
/// (publish/unpublish/archive/delete/category management) are further narrowed inside the handlers.
/// Thin controller — no business logic; every action dispatches a command/query.
/// </summary>
[ApiController]
[Route("api/v1/content/admin")]
[Tags("Content - Admin")]
[Authorize(Roles = ContentRoles.AdminAuthoring)]
public sealed class AdminContentController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    private readonly IAizenInfoAccessor _info;

    public AdminContentController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs, IAizenInfoAccessor info)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
        _info = info;
    }

    private long CurrentUserId => _info.UserInfoAccessor?.UserInfo?.UserId ?? 0;

    // ── Item authoring ────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<AizenApiResponse<ContentItemDto?>> Create(
        [FromBody] CreateContentItemRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemDto>(new CreateContentItemCommand
        {
            Type = body.Type,
            Slug = body.Slug,
            DefaultLanguage = body.DefaultLanguage,
            Translations = body.Translations,
            Tags = body.Tags,
            CategorySlug = body.CategorySlug,
            Placements = body.Placements,
            Audience = body.Audience,
            Media = body.Media,
            Campaign = body.Campaign,
            ReleaseNote = body.ReleaseNote,
            PublishAt = body.PublishAt,
            ExpireAt = body.ExpireAt,
            AuthorUserId = CurrentUserId,
        }, ct);
        return SetResponse(result);
    }

    [HttpPut("{id}")]
    public async Task<AizenApiResponse<ContentItemDto?>> Update(
        string id, [FromBody] UpdateContentItemRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemDto>(new UpdateContentItemCommand
        {
            ContentId = id,
            Slug = body.Slug,
            DefaultLanguage = body.DefaultLanguage,
            Tags = body.Tags,
            CategorySlug = body.CategorySlug,
            Campaign = body.Campaign,
            ReleaseNote = body.ReleaseNote,
        }, ct);
        return SetResponse(result);
    }

    [HttpPut("{id}/translations")]
    public async Task<AizenApiResponse<ContentItemDto?>> UpsertTranslation(
        string id, [FromBody] UpsertContentTranslationRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemDto>(new UpsertContentTranslationCommand
        {
            ContentId = id,
            Lang = body.Lang,
            Title = body.Title,
            Summary = body.Summary,
            Body = body.Body,
            SeoTitle = body.SeoTitle,
            SeoDescription = body.SeoDescription,
        }, ct);
        return SetResponse(result);
    }

    [HttpPut("{id}/placements")]
    public async Task<AizenApiResponse<ContentItemDto?>> SetPlacements(
        string id, [FromBody] SetContentPlacementsRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemDto>(new SetContentPlacementsCommand
        {
            ContentId = id,
            Placements = body.Placements,
        }, ct);
        return SetResponse(result);
    }

    [HttpPut("{id}/audience")]
    public async Task<AizenApiResponse<ContentItemDto?>> SetAudience(
        string id, [FromBody] SetContentAudienceRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemDto>(new SetContentAudienceCommand
        {
            ContentId = id,
            Audience = body.Audience,
        }, ct);
        return SetResponse(result);
    }

    [HttpPut("{id}/media")]
    public async Task<AizenApiResponse<ContentItemDto?>> AttachMedia(
        string id, [FromBody] AttachContentMediaRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemDto>(new AttachContentMediaCommand
        {
            ContentId = id,
            Media = body.Media,
        }, ct);
        return SetResponse(result);
    }

    [HttpPost("{id}/schedule")]
    public async Task<AizenApiResponse<ContentItemDto?>> Schedule(
        string id, [FromBody] ScheduleContentItemRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemDto>(new ScheduleContentItemCommand
        {
            ContentId = id,
            PublishAt = body.PublishAt,
            ExpireAt = body.ExpireAt,
        }, ct);
        return SetResponse(result);
    }

    [HttpPost("{id}/publish")]
    public async Task<AizenApiResponse<ContentItemDto?>> Publish(string id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemDto>(new PublishContentItemCommand { ContentId = id }, ct);
        return SetResponse(result);
    }

    [HttpPost("{id}/unpublish")]
    public async Task<AizenApiResponse<ContentItemDto?>> Unpublish(string id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemDto>(new UnpublishContentItemCommand { ContentId = id }, ct);
        return SetResponse(result);
    }

    [HttpPost("{id}/archive")]
    public async Task<AizenApiResponse<ContentItemDto?>> Archive(string id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemDto>(new ArchiveContentItemCommand { ContentId = id }, ct);
        return SetResponse(result);
    }

    [HttpDelete("{id}")]
    public async Task<AizenApiResponse<ContentItemDto?>> Delete(string id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemDto>(new DeleteContentItemCommand { ContentId = id }, ct);
        return SetResponse(result);
    }

    // ── Admin reads (all statuses) ────────────────────────────────────────────

    [HttpGet]
    public async Task<AizenApiResponse<ContentFeedResponse?>> List(
        [FromQuery] AdminContentListQueryParams query, [FromQuery] string lang = "tr", CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentFeedResponse>(new GetAdminContentListQuery
        {
            Type = query.Type,
            Status = query.Status,
            Surface = query.Surface,
            Tag = query.Tag,
            Lang = lang,
            Page = query.Page,
            PageSize = query.PageSize,
        }, ct);
        return SetResponse(result);
    }

    [HttpGet("{id}")]
    public async Task<AizenApiResponse<ContentItemDto?>> GetById(string id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemDto?>(new GetContentByIdAdminQuery { ContentId = id }, ct);
        return SetResponse(result);
    }

    // ── Category management (elevated, narrowed in-handler) ───────────────────

    [HttpPost("categories")]
    public async Task<AizenApiResponse<ContentCategoryDto?>> CreateCategory(
        [FromBody] CreateContentCategoryRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentCategoryDto>(new CreateContentCategoryCommand
        {
            Slug = body.Slug,
            Name = body.Name,
            ParentSlug = body.ParentSlug,
            Position = body.Position,
            IsActive = body.IsActive,
        }, ct);
        return SetResponse(result);
    }

    [HttpPut("categories/{slug}")]
    public async Task<AizenApiResponse<ContentCategoryDto?>> UpdateCategory(
        string slug, [FromBody] UpdateContentCategoryRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentCategoryDto>(new UpdateContentCategoryCommand
        {
            Slug = slug,
            Name = body.Name,
            ParentSlug = body.ParentSlug,
            Position = body.Position,
            IsActive = body.IsActive,
        }, ct);
        return SetResponse(result);
    }
}
