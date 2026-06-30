using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using Aizen.Modules.CargoDry.Repository.Persistence;
using MongoDB.Driver;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

[DocumentationInfo("CargoDry snapshot repository",
    "MongoDB repository for daily analytics snapshots. Injects CargoDryMongoDbContext " +
    "directly for upsert-by-DateKey semantics not available in IAizenMongoRepository.")]
public sealed class CargoDrySnapshotRepository : ICargoDrySnapshotRepository
{
    private readonly IMongoCollection<CargoDryKitUsageSnapshotDocument> _collection;

    public CargoDrySnapshotRepository(CargoDryMongoDbContext context)
    {
        _collection = context.Database
            .GetCollection<CargoDryKitUsageSnapshotDocument>("cargodry_usage_snapshots");
    }

    public Task UpsertAsync(CargoDryKitUsageSnapshotDocument document, CancellationToken ct)
    {
        var filter  = Builders<CargoDryKitUsageSnapshotDocument>.Filter.Eq(x => x.DateKey, document.DateKey);
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
        => _collection.Find(x => x.DateKey == dateKey).FirstOrDefaultAsync(ct)!;
}
