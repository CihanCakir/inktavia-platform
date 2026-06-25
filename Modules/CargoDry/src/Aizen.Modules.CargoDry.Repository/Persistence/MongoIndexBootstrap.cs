using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using MongoDB.Driver;

namespace Aizen.Modules.CargoDry.Repository.Persistence;

public static class MongoIndexBootstrap
{
    public static async Task EnsureIndexesAsync(IMongoDatabase database, CancellationToken ct = default)
    {
        var logs = database.GetCollection<CargoDryActivationLogDocument>("cargodry_activation_logs");

        await logs.Indexes.CreateManyAsync([
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys.Ascending(x => x.KitId)),
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys.Ascending(x => x.DateKey)),
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys
                    .Ascending(x => x.EventType)
                    .Ascending(x => x.DateKey)),
            new CreateIndexModel<CargoDryActivationLogDocument>(
                Builders<CargoDryActivationLogDocument>.IndexKeys
                    .Descending(x => x.OccurredAt)),
        ], ct);

        var snapshots = database.GetCollection<CargoDryKitUsageSnapshotDocument>("cargodry_usage_snapshots");

        await snapshots.Indexes.CreateOneAsync(
            new CreateIndexModel<CargoDryKitUsageSnapshotDocument>(
                Builders<CargoDryKitUsageSnapshotDocument>.IndexKeys.Descending(x => x.DateKey)),
            cancellationToken: ct);
    }
}
