using Aizen.Core.Data.Mongo.Document;
using Aizen.Modules.Vessel.Abstraction.Enum;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.Vessel.Domain.Documents;

[DocumentationInfo("Vessel profile read document", "MongoDB read-side denormalized document for fast vessel profile queries.")]
public sealed class VesselProfileReadDocument : AizenDocumentBase
{
    [BsonElement("vesselId")]
    public long VesselId { get; set; }

    [BsonElement("publicId")]
    public Guid? PublicId { get; set; }

    [BsonElement("vesselCode")]
    public string VesselCode { get; set; } = default!;

    [BsonElement("name")]
    public string Name { get; set; } = default!;

    [BsonElement("slug")]
    public string Slug { get; set; } = default!;

    [BsonElement("vesselTypeCode")]
    public string VesselTypeCode { get; set; } = default!;

    [BsonElement("flagCountryCode")]
    public string? FlagCountryCode { get; set; }

    [BsonElement("status")]
    public VesselStatus Status { get; set; }

    [BsonElement("visibility")]
    public VesselVisibility Visibility { get; set; }

    [BsonElement("isArchived")]
    public bool IsArchived { get; set; }

    [BsonElement("ownerUserIds")]
    public List<long> OwnerUserIds { get; set; } = new();

    [BsonElement("coverMediaUrl")]
    public string? CoverMediaUrl { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
