using Aizen.Core.Data.Mongo;
using Aizen.Core.Data.Mongo.Repository;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using Aizen.Modules.CargoDry.Repository.Persistence;
using MongoDB.Driver;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

[DocumentationInfo("CargoDry activation log repository",
    "MongoDB repository for kit lifecycle event audit logs.")]
public sealed class CargoDryActivationLogRepository : ICargoDryActivationLogRepository
{
    private readonly IAizenMongoRepository<CargoDryActivationLogDocument> _repo;

    public CargoDryActivationLogRepository(
        IAizenMongoRepositoryFactory<CargoDryMongoDbContext> factory)
    {
        _repo = factory.GetRepository<CargoDryActivationLogDocument>();
    }

    public Task InsertAsync(CargoDryActivationLogDocument document, CancellationToken ct)
        => _repo.AddAsync(document, ct);

    public async Task<List<CargoDryActivationLogDocument>> GetByKitIdAsync(long kitId, CancellationToken ct)
    {
        var filter = Builders<CargoDryActivationLogDocument>.Filter.Eq(x => x.KitId, kitId);
        var results = await _repo.WhereAsyncByFilterDefinition(filter);
        return results.OrderByDescending(x => x.OccurredAt).ToList();
    }

    public async Task<List<CargoDryActivationLogDocument>> GetByDateRangeAsync(
        string fromDateKey, string toDateKey, string? eventType, CancellationToken ct)
    {
        var filter = Builders<CargoDryActivationLogDocument>.Filter.And(
            Builders<CargoDryActivationLogDocument>.Filter.Gte(x => x.DateKey, fromDateKey),
            Builders<CargoDryActivationLogDocument>.Filter.Lte(x => x.DateKey, toDateKey)
        );

        if (!string.IsNullOrEmpty(eventType))
            filter &= Builders<CargoDryActivationLogDocument>.Filter.Eq(x => x.EventType, eventType);

        var results = await _repo.WhereAsyncByFilterDefinition(filter);
        return results.OrderByDescending(x => x.OccurredAt).ToList();
    }

    public async Task<Dictionary<string, int>> CountByEventTypeForDateAsync(string dateKey, CancellationToken ct)
    {
        var filter = Builders<CargoDryActivationLogDocument>.Filter.Eq(x => x.DateKey, dateKey);
        var results = await _repo.WhereAsyncByFilterDefinition(filter);
        return results
            .GroupBy(x => x.EventType)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<List<(string ProductCode, int Count)>> GetActivationsByProductAsync(
        string fromDateKey, string toDateKey, CancellationToken ct)
    {
        var filter = Builders<CargoDryActivationLogDocument>.Filter.And(
            Builders<CargoDryActivationLogDocument>.Filter.Gte(x => x.DateKey, fromDateKey),
            Builders<CargoDryActivationLogDocument>.Filter.Lte(x => x.DateKey, toDateKey),
            Builders<CargoDryActivationLogDocument>.Filter.Eq(x => x.EventType, "Activated")
        );

        var results = await _repo.WhereAsyncByFilterDefinition(filter);
        return results
            .GroupBy(x => x.ProductCode)
            .Select(g => (ProductCode: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();
    }
}
