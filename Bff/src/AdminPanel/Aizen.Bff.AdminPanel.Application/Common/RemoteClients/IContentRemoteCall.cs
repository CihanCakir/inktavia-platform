using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Abstraction.Model;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

/// <summary>
/// BFF-to-Content admin remote call. Covers the Content module's admin authoring surface
/// (AdminContentController — api/v1/content/admin) and comment moderation surface
/// (AdminContentCommentsController — api/v1/content/admin/comments), plus the single anonymous public
/// read the admin taxonomy panel needs (GetPublicCategoryTree) because the module exposes NO admin
/// category-list endpoint. Every route here maps 1:1 to a real Content controller route
/// (enforced by ContentRemoteCallRouteContractTests).
///
/// Typed bodies from Aizen.Modules.Content.Abstraction throughout — never object / JsonElement.
/// Admin routes are wrapped in AizenApiResponse&lt;T&gt;; the public categories read returns the bare DTO list
/// (PublicContentController is plain ControllerBase + Ok(result), no envelope).
///
/// Auth headers (Keycloak service token + X-Aizen-Bff-Assertion) are injected automatically by
/// AdminPanelBffAuthDelegatingHandler — never passed per-method.
/// </summary>
[DocumentationInfo("Content admin BFF remote call",
    "Synchronous BFF-to-Content calls for the editorial/blog authoring + comment-moderation surface. " +
    "Auth headers are injected automatically by AdminPanelBffAuthDelegatingHandler.")]
public interface IContentRemoteCall : IAizenRemoteCall
{
    // ─── Item reads (all statuses — the admin list is the only draft-visible enumeration) ───────────
    [AizenRemoteCallGet("/api/v1/content/admin")]
    Task<AizenApiResponse<ContentFeedResponse>> ListItems(
        [Query] ContentType? type = null,
        [Query] ContentStatus? status = null,
        [Query] ContentSurface? surface = null,
        [Query] string? tag = null,
        [Query] string lang = "tr",
        [Query] int page = 1,
        [Query] int pageSize = 20);

    [AizenRemoteCallGet("/api/v1/content/admin/{id}")]
    Task<AizenApiResponse<ContentItemDto>> GetItem(string id);

    // ─── Item authoring ─────────────────────────────────────────────────────────────────────────
    [AizenRemoteCallPost("/api/v1/content/admin")]
    Task<AizenApiResponse<ContentItemDto>> CreateItem([AizenRemoteCallBody] CreateContentItemRequest body);

    [AizenRemoteCallPut("/api/v1/content/admin/{id}")]
    Task<AizenApiResponse<ContentItemDto>> UpdateItem(string id, [AizenRemoteCallBody] UpdateContentItemRequest body);

    [AizenRemoteCallPut("/api/v1/content/admin/{id}/translations")]
    Task<AizenApiResponse<ContentItemDto>> UpsertTranslation(
        string id, [AizenRemoteCallBody] UpsertContentTranslationRequest body);

    [AizenRemoteCallPut("/api/v1/content/admin/{id}/media")]
    Task<AizenApiResponse<ContentItemDto>> SetMedia(string id, [AizenRemoteCallBody] AttachContentMediaRequest body);

    [AizenRemoteCallPut("/api/v1/content/admin/{id}/placements")]
    Task<AizenApiResponse<ContentItemDto>> SetPlacements(
        string id, [AizenRemoteCallBody] SetContentPlacementsRequest body);

    [AizenRemoteCallPut("/api/v1/content/admin/{id}/audience")]
    Task<AizenApiResponse<ContentItemDto>> SetAudience(string id, [AizenRemoteCallBody] SetContentAudienceRequest body);

    // ─── Lifecycle ──────────────────────────────────────────────────────────────────────────────
    [AizenRemoteCallPost("/api/v1/content/admin/{id}/schedule")]
    Task<AizenApiResponse<ContentItemDto>> Schedule(string id, [AizenRemoteCallBody] ScheduleContentItemRequest body);

    [AizenRemoteCallPost("/api/v1/content/admin/{id}/publish")]
    Task<AizenApiResponse<ContentItemDto>> Publish(string id);

    [AizenRemoteCallPost("/api/v1/content/admin/{id}/unpublish")]
    Task<AizenApiResponse<ContentItemDto>> Unpublish(string id);

    [AizenRemoteCallPost("/api/v1/content/admin/{id}/archive")]
    Task<AizenApiResponse<ContentItemDto>> Archive(string id);

    [AizenRemoteCallDelete("/api/v1/content/admin/{id}")]
    Task<AizenApiResponse<ContentItemDto>> Delete(string id);

    // ─── Categories (taxonomy) ────────────────────────────────────────────────────────────────────
    // Admin has NO category-list route; the anonymous public category tree is the only enumeration.
    [AizenRemoteCallGet("/api/v1/content/public/categories")]
    Task<List<ContentCategoryDto>> ListCategories([Query] string lang = "tr");

    [AizenRemoteCallPost("/api/v1/content/admin/categories")]
    Task<AizenApiResponse<ContentCategoryDto>> CreateCategory(
        [AizenRemoteCallBody] CreateContentCategoryRequest body);

    [AizenRemoteCallPut("/api/v1/content/admin/categories/{slug}")]
    Task<AizenApiResponse<ContentCategoryDto>> UpdateCategory(
        string slug, [AizenRemoteCallBody] UpdateContentCategoryRequest body);

    // ─── Comment moderation (per-content-item queue — the module has no global queue) ───────────────
    [AizenRemoteCallGet("/api/v1/content/admin/comments")]
    Task<AizenApiResponse<ContentCommentsResponse>> ListComments(
        [Query] string contentId,
        [Query] ContentCommentStatus? status = null,
        [Query] int page = 1,
        [Query] int pageSize = 20);

    [AizenRemoteCallPost("/api/v1/content/admin/comments/{commentId}/moderate")]
    Task<AizenApiResponse<ContentCommentDto>> ModerateComment(
        string commentId, [AizenRemoteCallBody] ModerateContentCommentRequest body);

    [AizenRemoteCallDelete("/api/v1/content/admin/comments/{commentId}")]
    Task<AizenApiResponse<ContentCommentDto>> DeleteComment(string commentId);
}
