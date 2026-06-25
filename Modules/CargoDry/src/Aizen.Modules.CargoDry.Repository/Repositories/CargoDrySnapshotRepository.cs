using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using MongoDB.Driver;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDrySnapshotRepository : ICargoDrySnapshotRepository
{
    private readonly IMongoCollection<CargoDryKitUsageSnapshotDocument> _collection;

    public CargoDrySnapshotRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<CargoDryKitUsageSnapshotDocument>("cargodry_usage_snapshots");
    }

    public Task UpsertAsync(CargoDryKitUsageSnapshotDocument document, CancellationToken ct)
    {
        var filter  = Builders<CargoDryKitUsageSnapshotDocument>.Filter.Eq(x => x.Id, document.DateKey);
        var options = new ReplaceOptions { IsUpsert = true };
        return _collection.ReplaceOneAsync(filter, document, options, ct);
    }

    public async Task<List<CargoDryKitUsageSnapshotDocument>> GetRecentAsync(int days, CancellationToken ct)
    {
        var fromDate = DateTimeOffset.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");
        return await _collection
            .Find(x => string.Compare(x.DateKey, fromDate, StringComparison.Ordinal) >= 0)
            .SortByDescending(x => x.DateKey)
            .ToListAsync(ct);
    }

    public Task<CargoDryKitUsageSnapshotDocument?> GetByDateKeyAsync(string dateKey, CancellationToken ct)
        => _collection.Find(x => x.Id == dateKey).FirstOrDefaultAsync(ct)!;
}
