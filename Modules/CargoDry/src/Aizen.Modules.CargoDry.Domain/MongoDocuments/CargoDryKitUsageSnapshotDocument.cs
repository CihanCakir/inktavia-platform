using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.CargoDry.Domain.MongoDocuments;

/// <summary>
/// Daily aggregated analytics snapshot computed by DailySnapshotJob.
/// One document per calendar date. Upserted daily at 00:05 UTC.
/// </summary>
public sealed class CargoDryKitUsageSnapshotDocument
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public string Id { get; set; } = default!;   // DateKey: "2026-06-25"

    public string DateKey       { get; set; } = default!;
    public DateTimeOffset ComputedAt { get; set; }

    public int Activations  { get; set; }
    public int Renewals     { get; set; }
    public int Expirations  { get; set; }
    public int Revocations  { get; set; }
    public int Extensions   { get; set; }

    public List<EfficiencyBucketSnapshot> EfficiencyBuckets { get; set; } = [];
    public List<ProductDaySnapshot>       ByProduct         { get; set; } = [];
}

public sealed class EfficiencyBucketSnapshot
{
    public string Bucket { get; set; } = default!;
    public int    Count  { get; set; }
}

public sealed class ProductDaySnapshot
{
    public string ProductCode { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public int    Activations { get; set; }
    public int    Renewals    { get; set; }
}
