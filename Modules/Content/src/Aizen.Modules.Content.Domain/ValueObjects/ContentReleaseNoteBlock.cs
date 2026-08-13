using Aizen.Modules.Content.Abstraction.Enum;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.Content.Domain.ValueObjects;

/// <summary>
/// Structured fields for Type=ReleaseNote only (§3). Informational; force-update gating is app-config.
/// </summary>
public sealed class ContentReleaseNoteBlock
{
    /// <summary>Which app the note targets, e.g. "Provider" | "MobileParticipant".</summary>
    public string? AppTarget { get; set; }

    /// <summary>Semantic version string, e.g. "1.4.0".</summary>
    public string? Version { get; set; }

    [BsonRepresentation(BsonType.String)]
    public ReleaseNotePlatform Platform { get; set; } = ReleaseNotePlatform.All;
}
