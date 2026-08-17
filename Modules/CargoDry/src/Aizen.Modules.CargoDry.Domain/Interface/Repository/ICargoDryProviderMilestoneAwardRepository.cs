using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDryProviderMilestoneAwardRepository
{
    Task<bool> ExistsAsync(long providerProfileId, string milestoneType, string periodKey, CancellationToken ct);
    Task AddAsync(CargoDryProviderMilestoneAwardEntity entity, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
