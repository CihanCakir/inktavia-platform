namespace Aizen.Modules.Content.Abstraction.Dto;

/// <summary>
/// Result of an add/remove-favorite toggle. Carries the post-operation favorite state and the
/// item's favorite count (post-operation value; eventually consistent under concurrent engagement).
/// </summary>
public sealed class ContentFavoriteResultDto
{
    public string ContentId { get; set; } = default!;
    public bool Favorited { get; set; }
    public int FavoriteCount { get; set; }
}
