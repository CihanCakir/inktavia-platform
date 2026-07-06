using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Domain.Entities.Performance;

namespace Aizen.Modules.Profile.Domain.Interface.Repository;

public interface IProfileMetricCacheRepository
{
    Task<ProfileMetricCacheEntity?> GetAsync(long profileId, ProfileType profileType, string metricKey, CancellationToken ct);
    Task<List<ProfileMetricCacheEntity>> GetAllForProfileAsync(long profileId, ProfileType profileType, CancellationToken ct);
    /// <summary>Removes expired cache entries for a profile.</summary>
    Task PurgeExpiredAsync(long profileId, ProfileType profileType, CancellationToken ct);
    Task AddAsync(ProfileMetricCacheEntity entity, CancellationToken ct);
    void Update(ProfileMetricCacheEntity entity);
    Task SaveChangesAsync(CancellationToken ct);
}
