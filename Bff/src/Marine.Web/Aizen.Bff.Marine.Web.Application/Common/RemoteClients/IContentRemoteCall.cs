using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Abstraction.Model;

namespace Aizen.Bff.Marine.Web.Application.Common.RemoteClients;

/// <summary>
/// BFF → Content module PUBLIC endpoints (api/v1/content/public/*). Anonymous on the module, still sent with
/// the service token by the shared auth handler.
///
/// Envelope discipline (verified against the shipped <c>PublicContentController</c>): every public endpoint returns
/// the RAW DTO via <c>Ok(dto)</c> — so the typed bodies here are the module DTOs directly (NOT
/// <c>AizenApiResponse&lt;T&gt;</c>). A not-visible slug returns HTTP 404, and any upstream failure comes back as a
/// non-2xx that Refit throws as an <c>ApiException</c> the feature handlers catch and re-throw as clean web errors.
///
/// The <c>surface</c> is a method parameter here, but the BFF pins it to <see cref="ContentSurface.MarineOsWeb"/>
/// in the handlers — the web caller never chooses a surface.
/// </summary>
public interface IContentRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/content/public/feed")]
    Task<ContentFeedResponse> GetPublicFeed(
        [Refit.Query] ContentSurface surface,
        [Refit.Query] string lang,
        [Refit.Query] int page,
        [Refit.Query] int pageSize);

    [AizenRemoteCallGet("/api/v1/content/public/by-type")]
    Task<ContentFeedResponse> GetPublicByType(
        [Refit.Query] ContentSurface surface,
        [Refit.Query] ContentType type,
        [Refit.Query] string lang,
        [Refit.Query] int page,
        [Refit.Query] int pageSize);

    [AizenRemoteCallGet("/api/v1/content/public/items/{slug}")]
    Task<ContentItemDto> GetPublicBySlug(string slug, [Refit.Query] string lang);

    [AizenRemoteCallGet("/api/v1/content/public/categories")]
    Task<List<ContentCategoryDto>> GetPublicCategories([Refit.Query] string lang);

    [AizenRemoteCallGet("/api/v1/content/public/items/{contentId}/comments")]
    Task<ContentCommentsResponse> GetPublicComments(
        string contentId,
        [Refit.Query] int page,
        [Refit.Query] int pageSize);

    // ── Participant engagement (api/v1/content/me/*) ──────────────────────────────────────────────────
    // These require the BFF identity assertion (X-Aizen-Bff-Assertion + X-Aizen-User-Id) — attached by the auth
    // handler once the identity holder is resolved. UNLIKE the public endpoints above, the /me controller wraps
    // every reply in AizenApiResponse<T> (verified against the shipped MeContentController) — so bind the envelope.

    [AizenRemoteCallPost("/api/v1/content/me/items/{contentId}/comments")]
    Task<AizenApiResponse<ContentCommentDto>> AddComment(
        string contentId, [AizenRemoteCallBody] AddContentCommentRequest request);

    [AizenRemoteCallPost("/api/v1/content/me/items/{contentId}/favorite")]
    Task<AizenApiResponse<ContentFavoriteResultDto>> AddFavorite(string contentId);

    [AizenRemoteCallDelete("/api/v1/content/me/items/{contentId}/favorite")]
    Task<AizenApiResponse<ContentFavoriteResultDto>> RemoveFavorite(string contentId);

    [AizenRemoteCallGet("/api/v1/content/me/favorites")]
    Task<AizenApiResponse<ContentFavoritesResponse>> GetMyFavorites(
        [Refit.Query] string lang, [Refit.Query] int page, [Refit.Query] int pageSize);

    [AizenRemoteCallGet("/api/v1/content/me/items/{contentId}/my-comments")]
    Task<AizenApiResponse<List<ContentCommentDto>>> GetMyComments(string contentId);
}
