namespace Aizen.Modules.Content.Abstraction.Dto;

/// <summary>
/// Paged wrapper for a surface-scoped, localized content feed (§8 GetPublicContentFeed).
/// Also reusable for admin/list read paths that return summary rows.
/// </summary>
public sealed class ContentFeedResponse
{
    public List<ContentItemSummaryDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long Total { get; set; }
}
