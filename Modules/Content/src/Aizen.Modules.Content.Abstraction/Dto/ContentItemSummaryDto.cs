using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Abstraction.Dto;

/// <summary>
/// Lean list/feed row for a content item. Title/Summary are resolved to a single requested
/// language (falling back to the item's DefaultLanguage); CoverUrl is the cover media URL.
/// </summary>
public sealed class ContentItemSummaryDto
{
    public string Id { get; set; } = default!;
    public ContentType Type { get; set; }
    public string Slug { get; set; } = default!;
    public ContentStatus Status { get; set; }

    /// <summary>Language the Title/Summary below were resolved to.</summary>
    public string Lang { get; set; } = default!;

    /// <summary>
    /// Every language this item is translated into (from the item's translation set). Additive (W3.1): lets a
    /// consumer emit hreflang alternates for exactly the locales the item exists in, without a per-locale slug.
    /// </summary>
    public List<string> AvailableLangs { get; set; } = new();

    public string Title { get; set; } = default!;
    public string? Summary { get; set; }
    public string? CoverUrl { get; set; }

    public string? CategorySlug { get; set; }
    public List<string> Tags { get; set; } = new();

    public int CommentCount { get; set; }
    public int FavoriteCount { get; set; }

    public DateTimeOffset? PublishAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
