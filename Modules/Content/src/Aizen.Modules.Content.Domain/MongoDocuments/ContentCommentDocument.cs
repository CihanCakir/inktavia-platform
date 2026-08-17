using Aizen.Core.Data.Mongo.Attributes;
using Aizen.Core.Data.Mongo.Document;
using Aizen.Modules.Content.Abstraction.Enum;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.Content.Domain.MongoDocuments;

/// <summary>
/// A participant comment on a content item (§5.2). Kept in its own collection because it grows
/// unbounded. Supports optional single-level threads via ParentCommentId.
/// Stored in MongoDB collection: content_comments.
/// </summary>
[AizenCollectionInfo(CollectionName = "content_comments")]
public sealed class ContentCommentDocument : AizenDocumentBase
{
    /// <summary>Id of the parent <see cref="ContentItemDocument"/>.</summary>
    public string ContentId { get; set; } = default!;

    public long AuthorUserId { get; set; }
    public long? AuthorProfileId { get; set; }
    public string? AuthorDisplayName { get; set; }

    public string Body { get; set; } = default!;

    [BsonRepresentation(BsonType.String)]
    public ContentCommentStatus Status { get; set; } = ContentCommentStatus.Pending;

    /// <summary>Optional single-level thread parent (null for a top-level comment).</summary>
    public string? ParentCommentId { get; set; }

    // ── Moderation trail (C9) ───────────────────────────────────────────────────
    public long? ModeratedByUserId { get; set; }
    public DateTimeOffset? ModeratedAt { get; set; }
    public string? LastModerationReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
