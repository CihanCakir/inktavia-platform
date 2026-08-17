using Aizen.Modules.Content.Abstraction.Enum;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.Content.Domain.ValueObjects;

/// <summary>
/// Where a content item renders — one entry per (surface, slot). The Placement/Surface axis (§3).
/// A content item may carry many placements at once.
/// </summary>
public sealed class ContentPlacement
{
    [BsonRepresentation(BsonType.String)]
    public ContentSurface Surface { get; set; }

    [BsonRepresentation(BsonType.String)]
    public ContentSlot Slot { get; set; }

    public int Position { get; set; }

    /// <summary>Pin (sticky) this placement until the given instant; null when not pinned.</summary>
    public DateTimeOffset? PinnedUntil { get; set; }
}
