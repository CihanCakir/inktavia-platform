using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using MongoDB.Driver;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDryActivationLogRepository : ICargoDryActivationLogRepository
{
    private readonly IMongoCollection<CargoDryActivationLogDocument> _collection;

    public CargoDryActivationLogRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<CargoDryActivationLogDocument>("cargodry_activation_logs");
    }

    public Task InsertAsync(CargoDryActivationLogDocument document, CancellationToken ct)
        => _collection.InsertOneAsync(document, cancellationToken: ct);

    public async Task<List<CargoDryActivationLogDocument>> GetByKitIdAsync(long kitId, CancellationToken ct)
        => await _collection
            .Find(x => x.KitId == kitId)
            .SortByDescending(x => x.OccurredAt)
            .ToListAsync(ct);

    public async Task<List<CargoDryActivationLogDocument>> GetByDateRangeAsync(
        string fromDateKey, string toDateKey, string? eventType, CancellationToken ct)
    {
        var filter = Builders<CargoDryActivationLogDocument>.Filter.And(
            Builders<CargoDryActivationLogDocument>.Filter.Gte(x => x.DateKey, fromDateKey),
            Builders<CargoDryActivationLogDocument>.Filter.Lte(x => x.DateKey, toDateKey)
        );

        if (!string.IsNullOrEmpty(eventType))
            filter &= Builders<CargoDryActivationLogDocument>.Filter.Eq(x => x.EventType, eventType);

        return await _collection.Find(filter).SortByDescending(x => x.OccurredAt).ToListAsync(ct);
    }

    public async Task<Dictionary<string, int>> CountByEventTypeForDateAsync(string dateKey, CancellationToken ct)
    {
        var docs = await _collection
            .Find(x => x.DateKey == dateKey)
            .Project(x => x.EventType)
            .ToListAsync(ct);

        return docs.GroupBy(e => e).ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<List<(string ProductCode, int Count)>> GetActivationsByProductAsync(
        string fromDateKey, string toDateKey, CancellationToken ct)
    {
        var filter = Builders<CargoDryActivationLogDocument>.Filter.And(
            Builders<CargoDryActivationLogDocument>.Filter.Gte(x => x.DateKey, fromDateKey),
            Builders<CargoDryActivationLogDocument>.Filter.Lte(x => x.DateKey, toDateKey),
            Builders<CargoDryActivationLogDocument>.Filter.Eq(x => x.EventType, "Activated")
        );

        var docs = await _collection
            .Find(filter)
            .Project(x => x.ProductCode)
            .ToListAsync(ct);

        return docs.GroupBy(p => p)
            .Select(g => (ProductCode: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();
    }
}
