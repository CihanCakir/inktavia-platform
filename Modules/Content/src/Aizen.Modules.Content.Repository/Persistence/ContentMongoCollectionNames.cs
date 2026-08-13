namespace Aizen.Modules.Content.Repository.Persistence;

/// <summary>
/// Canonical MongoDB collection names for the Content module (§5).
/// Kept in one place so the context, index initializer and repositories agree.
/// </summary>
public static class ContentMongoCollectionNames
{
    public const string Items      = "content_items";
    public const string Comments   = "content_comments";
    public const string Favorites  = "content_favorites";
    public const string Categories = "content_categories";
}
