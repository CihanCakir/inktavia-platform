using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Bff.Marine.Web.Application.Contracts.Content;

/// <summary>
/// The participant's OWN comment (from add-comment / my-comments). A web-facing reshape of the module
/// <c>ContentCommentDto</c>: keeps the caller's own moderation <see cref="Status"/> (Pending/Approved/…) so they
/// can see whether their comment is live, but OMITS the internal author ids (AuthorUserId/AuthorProfileId) and the
/// moderator trail (ModeratedByUserId/LastModerationReason) — a participant must not see who moderated or why.
/// </summary>
public sealed class WebMyCommentDto
{
    public string Id { get; set; } = default!;
    public string ContentId { get; set; } = default!;
    public string Body { get; set; } = default!;
    public ContentCommentStatus Status { get; set; }
    public string? ParentCommentId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// A participant favorite (from my-favorites). Reshaped from the module <c>ContentFavoriteDto</c> — OMITS the
/// internal UserId/ProfileId; keeps the id/contentId/timestamp and the (already web-appropriate) content summary.
/// </summary>
public sealed class WebMyFavoriteDto
{
    public string Id { get; set; } = default!;
    public string ContentId { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public ContentItemSummaryDto? Content { get; set; }
}

/// <summary>Paged wrapper for a participant's favorites.</summary>
public sealed class WebMyFavoritesResponse
{
    public List<WebMyFavoriteDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long Total { get; set; }
}
