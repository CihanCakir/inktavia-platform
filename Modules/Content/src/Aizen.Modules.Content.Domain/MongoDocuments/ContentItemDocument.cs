using Aizen.Core.Data.Mongo.Attributes;
using Aizen.Core.Data.Mongo.Document;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.Content.Domain.MongoDocuments;

/// <summary>
/// Aggregate root for a single content item (§5.1). Embeds everything read together
/// (translations, media, placements, audience) and keeps engagement counters denormalized.
/// Stored in MongoDB collection: content_items.
/// </summary>
[AizenCollectionInfo(CollectionName = "content_items")]
public sealed class ContentItemDocument : AizenDocumentBase
{
    // AizenDocumentBase provides:
    //   - string Id (ObjectId)
    //   - bool IsDeleted (soft-delete global filter)
    // Do NOT add [BsonId] or [BsonRepresentation] on Id — they are inherited.

    [BsonRepresentation(BsonType.String)]
    public ContentType Type { get; set; }

    /// <summary>URL-safe unique identifier for public addressing.</summary>
    public string Slug { get; set; } = default!;

    [BsonRepresentation(BsonType.String)]
    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    public DateTimeOffset? PublishAt { get; set; }
    public DateTimeOffset? ExpireAt { get; set; }

    /// <summary>Calendar date string (yyyy-MM-dd) of PublishAt, for efficient range queries.</summary>
    public string? DateKey { get; set; }

    /// <summary>Fallback language code when a requested translation is missing.</summary>
    public string DefaultLanguage { get; set; } = "tr";

    public List<ContentTranslation> Translations { get; set; } = new();
    public List<ContentMediaRef> Media { get; set; } = new();
    public List<ContentPlacement> Placements { get; set; } = new();
    public ContentAudience Audience { get; set; } = new();

    public List<string> Tags { get; set; } = new();
    public string? CategorySlug { get; set; }

    /// <summary>Presentation block — populated only when Type == Campaign (B1).</summary>
    public ContentCampaignBlock? Campaign { get; set; }

    /// <summary>Structured block — populated only when Type == ReleaseNote (§3).</summary>
    public ContentReleaseNoteBlock? ReleaseNote { get; set; }

    // ── Denormalized engagement counters (maintained on engagement changes) ──────
    public int CommentCount { get; set; }
    public int FavoriteCount { get; set; }

    // ── Audit ────────────────────────────────────────────────────────────────────
    public long AuthorUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAt { get; set; }
}
