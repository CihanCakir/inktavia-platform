using Aizen.Core.Data.Mongo.Document;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.Vessel.Domain.Documents;

[DocumentationInfo("Vessel media metadata document", "MongoDB document for flexible media metadata storage.")]
public sealed class VesselMediaMetadataDocument : AizenDocumentBase
{
    [BsonElement("vesselId")]
    public long VesselId { get; set; }

    [BsonElement("mediaType")]
    public VesselMediaType MediaType { get; set; }

    [BsonElement("fileUrl")]
    public string? FileUrl { get; set; }

    [BsonElement("isCover")]
    public bool IsCover { get; set; }

    [BsonElement("sortOrder")]
    public int SortOrder { get; set; }
}
