using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Bff.Marine.Web.Application.Contracts.Content;

/// <summary>
/// Public content detail for the website (by-slug). A web-facing reshape of the module <c>ContentItemDto</c>:
/// translations are resolved to the requested language, and the internal fields the module carries but the
/// anonymous public must not see are OMITTED — AuthorUserId, Placements, Audience (targeting), ExpireAt,
/// UpdatedAt, Status, and the full multi-language translation set.
/// </summary>
public sealed class WebContentDetailDto
{
    public string Id { get; set; } = default!;
    public ContentType Type { get; set; }
    public string Slug { get; set; } = default!;

    /// <summary>Language the Title/Summary/Body below were resolved to.</summary>
    public string Lang { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Summary { get; set; }
    public string? Body { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }

    public string? CoverUrl { get; set; }
    public List<WebContentMediaDto> Gallery { get; set; } = new();

    public List<string> Tags { get; set; } = new();
    public string? CategorySlug { get; set; }

    public WebContentCampaignDto? Campaign { get; set; }
    public WebContentReleaseNoteDto? ReleaseNote { get; set; }

    public int CommentCount { get; set; }
    public int FavoriteCount { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}

/// <summary>A media reference surfaced to the website (URL + alt), without the FileStorage id.</summary>
public sealed class WebContentMediaDto
{
    public string? Url { get; set; }
    public string? Alt { get; set; }
    public ContentMediaKind Kind { get; set; }
    public int Position { get; set; }
}

/// <summary>Campaign CTA block (Type=Campaign only). ExternalRef flattened to system/key.</summary>
public sealed class WebContentCampaignDto
{
    public string? CtaLabel { get; set; }
    public string? CtaUrl { get; set; }
    public string? ExternalSystem { get; set; }
    public string? ExternalKey { get; set; }
}

/// <summary>Release-note block (Type=ReleaseNote only).</summary>
public sealed class WebContentReleaseNoteDto
{
    public string? AppTarget { get; set; }
    public string? Version { get; set; }
    public ReleaseNotePlatform Platform { get; set; }
}

/// <summary>
/// A public approved comment for the website. A web-facing reshape of the module <c>ContentCommentDto</c> that
/// OMITS the internal author ids (AuthorUserId/AuthorProfileId), the status, and the moderation trail
/// (ModeratedByUserId/ModeratedAt/LastModerationReason) — none of which the anonymous public may see.
/// </summary>
public sealed class WebContentCommentDto
{
    public string Id { get; set; } = default!;
    public string? AuthorDisplayName { get; set; }
    public string Body { get; set; } = default!;
    public string? ParentCommentId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Paged wrapper for public approved comments.</summary>
public sealed class WebContentCommentsResponse
{
    public List<WebContentCommentDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long Total { get; set; }
}
