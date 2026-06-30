using Aizen.Core.Data.Mongo.Document;
using Aizen.Modules.Vessel.Abstraction.Enum;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.Vessel.Domain.Documents;

[DocumentationInfo("Vessel audit snapshot document", "MongoDB document for archiving full vessel state at a point in time.")]
public sealed class VesselAuditSnapshotDocument : AizenDocumentBase
{
    [BsonElement("vesselId")]
    public long VesselId { get; set; }

    [BsonElement("snapshotAt")]
    public DateTime SnapshotAt { get; set; } = DateTime.UtcNow;

    [BsonElement("triggeredByUserId")]
    public long? TriggeredByUserId { get; set; }

    [BsonElement("reason")]
    public string? Reason { get; set; }

    [BsonElement("statusBefore")]
    public VesselStatus? StatusBefore { get; set; }

    [BsonElement("statusAfter")]
    public VesselStatus? StatusAfter { get; set; }

    [BsonElement("payload")]
    public string? Payload { get; set; }
}
