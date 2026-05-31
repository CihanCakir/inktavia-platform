using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class ReferenceDataSeedService : IReferenceDataSeedService
{
    private readonly IReferenceDataMongoIndexService _indexService;

    public ReferenceDataSeedService(IReferenceDataMongoIndexService indexService)
    {
        _indexService = indexService;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _indexService.EnsureIndexesAsync(cancellationToken);
    }
}
