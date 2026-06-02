using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Documents;
using Aizen.Modules.Vessel.Repository.Persistence;
using MongoDB.Driver;

namespace Aizen.Modules.Vessel.Repository.Mongo;

[DocumentationInfo("Vessel profile read repository", "MongoDB repository for vessel profile read documents.")]
public sealed class VesselProfileReadRepository
{
    private readonly IMongoCollection<VesselProfileReadDocument> _collection;

    public VesselProfileReadRepository(VesselMongoDbContext mongoDbContext)
    {
        _collection = mongoDbContext.Database.GetCollection<VesselProfileReadDocument>(
            VesselMongoCollectionNames.VesselProfileReads);
    }

    public Task<VesselProfileReadDocument?> GetByVesselIdAsync(long vesselId, CancellationToken ct = default)
        => _collection.Find(x => x.VesselId == vesselId && !x.IsDeleted).FirstOrDefaultAsync(ct)!;

    public async Task UpsertAsync(VesselProfileReadDocument document, CancellationToken ct = default)
    {
        var filter = Builders<VesselProfileReadDocument>.Filter.Eq(x => x.VesselId, document.VesselId);
        await _collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true }, ct);
    }

    public async Task MarkDeletedAsync(long vesselId, CancellationToken ct = default)
    {
        var filter = Builders<VesselProfileReadDocument>.Filter.Eq(x => x.VesselId, vesselId);
        var update = Builders<VesselProfileReadDocument>.Update.Set(x => x.IsDeleted, true);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }
}
