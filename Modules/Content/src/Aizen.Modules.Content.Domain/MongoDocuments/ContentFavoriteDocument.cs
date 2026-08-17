using Aizen.Core.Data.Mongo.Attributes;
using Aizen.Core.Data.Mongo.Document;

namespace Aizen.Modules.Content.Domain.MongoDocuments;

/// <summary>
/// A participant's favorite/bookmark of a content item (§5.3). Exactly one per (content, user)
/// via a unique index, so favoriting is idempotent.
/// Stored in MongoDB collection: content_favorites.
/// </summary>
[AizenCollectionInfo(CollectionName = "content_favorites")]
public sealed class ContentFavoriteDocument : AizenDocumentBase
{
    /// <summary>Id of the favorited <see cref="ContentItemDocument"/>.</summary>
    public string ContentId { get; set; } = default!;

    public long UserId { get; set; }
    public long? ProfileId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
