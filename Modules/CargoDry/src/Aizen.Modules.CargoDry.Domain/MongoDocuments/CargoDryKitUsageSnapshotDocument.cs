using Aizen.Core.Data.Mongo.Attributes;
using Aizen.Core.Data.Mongo.Document;

namespace Aizen.Modules.CargoDry.Domain.MongoDocuments;

/// <summary>
/// Daily aggregated analytics snapshot computed by DailySnapshotJob.
/// One document per calendar date. Upserted daily at 00:05 UTC.
/// DateKey is the business unique key (unique index enforced).
/// Id is a standard ObjectId (from AizenDocumentBase).
/// </summary>
[AizenCollectionInfo(CollectionName = "cargodry_usage_snapshots")]
public sealed class CargoDryKitUsageSnapshotDocument : AizenDocumentBase
{
    // AizenDocumentBase provides Id (ObjectId) and IsDeleted.
    // DateKey is the business key — unique index defined in CargoDryMongoIndexInitializer.

    public string DateKey        { get; set; } = default!;
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
