using Aizen.Modules.CargoDry.Abstraction.Model;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using MongoDB.Driver;

namespace Aizen.Modules.CargoDry.Repository.Persistence;

[DocumentationInfo("CargoDry MongoDB index initializer",
    "Ensures required indexes on cargodry_activation_logs and cargodry_usage_snapshots.")]
public sealed class CargoDryMongoIndexInitializer
{
    private readonly IMongoDatabase _db;

    public CargoDryMongoIndexInitializer(CargoDryMongoDbContext context)
    {
        _db = context.Database;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await CreateActivationLogIndexesAsync(ct);
        await CreateSnapshotIndexesAsync(ct);
    }

    private Task CreateActivationLogIndexesAsync(CancellationToken ct)
    {
        var col = _db.GetCollection<CargoDryActivationLogDocument>("cargodry_activation_logs");
        return col.Indexes.CreateManyAsync([
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys.Ascending(x => x.KitId),
                new CreateIndexOptions { Name = "ix_kit_id" }),
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys.Ascending(x => x.DateKey),
                new CreateIndexOptions { Name = "ix_date_key" }),
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys
                    .Ascending(x => x.EventType)
                    .Ascending(x => x.DateKey),
                new CreateIndexOptions { Name = "ix_event_type_date" }),
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys.Descending(x => x.OccurredAt),
                new CreateIndexOptions { Name = "ix_occurred_at_desc" }),
        ], ct);
    }

    private Task CreateSnapshotIndexesAsync(CancellationToken ct)
    {
        var col = _db.GetCollection<CargoDryKitUsageSnapshotDocument>("cargodry_usage_snapshots");
        return col.Indexes.CreateOneAsync(
            new CreateIndexModel<CargoDryKitUsageSnapshotDocument>(
                Builders<CargoDryKitUsageSnapshotDocument>.IndexKeys.Descending(x => x.DateKey),
                new CreateIndexOptions { Unique = true, Name = "ux_date_key" }),
            cancellationToken: ct);
    }
}
