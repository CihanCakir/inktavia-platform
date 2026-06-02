namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

public interface IReferenceDataSeedService
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
