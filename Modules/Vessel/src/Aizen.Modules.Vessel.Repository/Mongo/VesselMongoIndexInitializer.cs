using Aizen.Modules.Vessel.Domain.Documents;
using Aizen.Modules.Vessel.Repository.Persistence;
using MongoDB.Driver;

namespace Aizen.Modules.Vessel.Repository.Mongo;

public sealed class VesselMongoIndexInitializer
{
    private readonly IMongoDatabase _mongoDatabase;

    public VesselMongoIndexInitializer(VesselMongoDbContext mongoDbContext)
    {
        _mongoDatabase = mongoDbContext.Database;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await CreateVesselProfileReadsIndexesAsync(cancellationToken);
    }

    private async Task CreateVesselProfileReadsIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _mongoDatabase.GetCollection<VesselProfileReadDocument>(VesselMongoCollectionNames.VesselProfileReads);

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<VesselProfileReadDocument>(
                Builders<VesselProfileReadDocument>.IndexKeys.Ascending(x => x.VesselId),
                new CreateIndexOptions { Unique = true, Name = "ux_vessel_id" }),
            cancellationToken: cancellationToken);

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<VesselProfileReadDocument>(
                Builders<VesselProfileReadDocument>.IndexKeys.Ascending(x => x.OwnerUserIds),
                new CreateIndexOptions { Name = "ix_owner_user_ids" }),
            cancellationToken: cancellationToken);

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<VesselProfileReadDocument>(
                Builders<VesselProfileReadDocument>.IndexKeys.Ascending(x => x.Status),
                new CreateIndexOptions { Name = "ix_status" }),
            cancellationToken: cancellationToken);
    }
}
