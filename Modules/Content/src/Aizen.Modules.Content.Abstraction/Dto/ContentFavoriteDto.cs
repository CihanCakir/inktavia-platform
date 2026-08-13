namespace Aizen.Modules.Content.Abstraction.Dto;

/// <summary>Read model of a participant's favorite of a content item (§5.3).</summary>
public sealed class ContentFavoriteDto
{
    public string Id { get; set; } = default!;
    public string ContentId { get; set; } = default!;
    public long UserId { get; set; }
    public long? ProfileId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Optional lean summary of the favorited item, for "my favorites" list rendering.</summary>
    public ContentItemSummaryDto? Content { get; set; }
}
