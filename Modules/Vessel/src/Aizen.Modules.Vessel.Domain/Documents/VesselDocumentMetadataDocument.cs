using Aizen.Core.Data.Mongo.Document;
using Aizen.Modules.Vessel.Abstraction.Enum;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.Vessel.Domain.Documents;

[DocumentationInfo("Vessel document metadata document", "MongoDB document for flexible document metadata storage.")]
public sealed class VesselDocumentMetadataDocument : AizenDocumentBase
{
    [BsonElement("vesselId")]
    public long VesselId { get; set; }

    [BsonElement("documentTypeCode")]
    public string DocumentTypeCode { get; set; } = default!;

    [BsonElement("documentName")]
    public string DocumentName { get; set; } = default!;

    [BsonElement("fileUrl")]
    public string? FileUrl { get; set; }

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [BsonElement("status")]
    public VesselDocumentStatus Status { get; set; }
}
