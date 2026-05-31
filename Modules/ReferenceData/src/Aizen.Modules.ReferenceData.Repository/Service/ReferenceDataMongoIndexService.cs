using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Modules.ReferenceData.Repository.Mongo;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class ReferenceDataMongoIndexService : IReferenceDataMongoIndexService
{
    private readonly ReferenceDataMongoIndexInitializer _initializer;

    public ReferenceDataMongoIndexService(ReferenceDataMongoIndexInitializer initializer)
    {
        _initializer = initializer;
    }

    public Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
        => _initializer.InitializeAsync(cancellationToken);
}
