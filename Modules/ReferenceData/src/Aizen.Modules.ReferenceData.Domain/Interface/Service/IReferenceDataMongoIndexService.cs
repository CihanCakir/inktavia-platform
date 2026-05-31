namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

public interface IReferenceDataMongoIndexService
{
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
}
