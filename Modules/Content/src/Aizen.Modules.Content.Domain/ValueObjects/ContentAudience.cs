using Aizen.Modules.Content.Abstraction.Enum;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.Content.Domain.ValueObjects;

/// <summary>
/// Who may see a content item — the Audience/Segment axis (§3). Region/city codes are filters only;
/// no live geo/radius discovery here (B5).
/// </summary>
public sealed class ContentAudience
{
    [BsonRepresentation(BsonType.String)]
    public ContentAudienceType Type { get; set; } = ContentAudienceType.Public;

    public List<string> RegionCodes { get; set; } = new();
    public List<string> CityCodes { get; set; } = new();
    public List<string> Tiers { get; set; } = new();
}
