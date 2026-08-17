using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Abstraction.Model;

/// <summary>Generic paging parameters (1-based page). Reused by comment/favorite list reads.</summary>
public sealed class PagedQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Query parameters for the public content feed (§8 GetPublicContentFeed): surface + language + paging,
/// with optional type/category/tag narrowing. Audience is resolved server-side (Public for anonymous).
/// </summary>
public sealed class ContentFeedQueryParams
{
    public ContentSurface Surface { get; set; }
    public string Lang { get; set; } = "tr";

    public ContentType? Type { get; set; }
    public string? CategorySlug { get; set; }
    public string? Tag { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Query parameters for the admin content list (§8 GetAdminContentList): filter by type/status/surface/tag, paged.
/// </summary>
public sealed class AdminContentListQueryParams
{
    public ContentType? Type { get; set; }
    public ContentStatus? Status { get; set; }
    public ContentSurface? Surface { get; set; }
    public string? Tag { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
