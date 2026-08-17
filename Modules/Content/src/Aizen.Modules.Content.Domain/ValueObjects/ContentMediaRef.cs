using Aizen.Modules.Content.Abstraction.Enum;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.Content.Domain.ValueObjects;

/// <summary>
/// Reference to a media asset stored by FileStorage (B3). Content keeps the id/URL, never bytes.
/// </summary>
public sealed class ContentMediaRef
{
    /// <summary>FileStorage asset id (source of truth for the binary).</summary>
    public string FileStorageId { get; set; } = default!;

    /// <summary>Resolved/CDN URL for direct rendering (may be refreshed from FileStorage).</summary>
    public string? Url { get; set; }

    [BsonRepresentation(BsonType.String)]
    public ContentMediaKind Kind { get; set; }

    public string? Alt { get; set; }
    public int Position { get; set; }
}
