using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.CargoDry.Domain.MongoDocuments;

/// <summary>
/// Append-only audit record for every kit lifecycle event (activation, revocation, expiry, renewal).
/// Stored in MongoDB collection: cargodry_activation_logs.
/// </summary>
public sealed class CargoDryActivationLogDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public long   KitId        { get; set; }
    public string SerialNumber { get; set; } = default!;
    public string KitCode      { get; set; } = default!;
    public string ProductCode  { get; set; } = default!;
    public string BatchCode    { get; set; } = default!;

    /// <summary>Type of event: "Activated" | "Revoked" | "Expired" | "Renewed" | "Extended"</summary>
    public string EventType    { get; set; } = default!;

    public long?  OwnerUserId       { get; set; }
    public long?  VesselId          { get; set; }
    public string? ActivationMethod { get; set; }
    public string? ActivationSource { get; set; }
    public string? DeviceInfo       { get; set; }
    public string? IpAddress        { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public string? RevokeReason { get; set; }

    public int?    AddedDays    { get; set; }
    public string? RenewalType  { get; set; }
    public string? PaymentRef   { get; set; }

    public Dictionary<string, object>? TelemetryPayload { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Calendar date string (YYYY-MM-DD) for efficient date-range queries.</summary>
    public string DateKey { get; set; } = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
}
