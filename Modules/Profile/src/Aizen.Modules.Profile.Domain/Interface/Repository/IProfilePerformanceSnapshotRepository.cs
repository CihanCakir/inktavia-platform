using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Domain.Entities.Performance;

namespace Aizen.Modules.Profile.Domain.Interface.Repository;

public interface IProfilePerformanceSnapshotRepository
{
    Task<ProfilePerformanceSnapshotEntity?> GetByProfileAsync(long profileId, ProfileType profileType, CancellationToken ct);
    Task<List<ProfilePerformanceSnapshotEntity>> GetByTierAsync(PriorityTier tier, int skip, int take, CancellationToken ct);
    Task<List<ProfilePerformanceSnapshotEntity>> GetWithActiveRiskSignalsAsync(int skip, int take, CancellationToken ct);
    Task<int> CountByTierAsync(PriorityTier tier, CancellationToken ct);
    Task AddAsync(ProfilePerformanceSnapshotEntity entity, CancellationToken ct);
    void Update(ProfilePerformanceSnapshotEntity entity);
    Task SaveChangesAsync(CancellationToken ct);
}
