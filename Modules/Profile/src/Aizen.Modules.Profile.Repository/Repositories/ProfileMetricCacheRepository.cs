using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Domain.Entities.Performance;
using Aizen.Modules.Profile.Domain.Interface.Repository;
using Aizen.Modules.Profile.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Profile.Repository.Repositories;

public sealed class ProfileMetricCacheRepository : IProfileMetricCacheRepository
{
    private readonly ProfileDbContext _db;
    public ProfileMetricCacheRepository(ProfileDbContext db) => _db = db;

    public Task<ProfileMetricCacheEntity?> GetAsync(
        long profileId, ProfileType profileType, string metricKey, CancellationToken ct)
        => _db.MetricCaches
              .FirstOrDefaultAsync(x => x.ProfileId == profileId
                                     && x.ProfileType == profileType
                                     && x.MetricKey == metricKey, ct);

    public Task<List<ProfileMetricCacheEntity>> GetAllForProfileAsync(
        long profileId, ProfileType profileType, CancellationToken ct)
        => _db.MetricCaches
              .Where(x => x.ProfileId == profileId && x.ProfileType == profileType)
              .ToListAsync(ct);

    public async Task PurgeExpiredAsync(long profileId, ProfileType profileType, CancellationToken ct)
    {
        var now     = DateTime.UtcNow;
        var expired = await _db.MetricCaches
            .Where(x => x.ProfileId == profileId
                     && x.ProfileType == profileType
                     && x.ExpiresAtUtc.HasValue
                     && x.ExpiresAtUtc.Value < now)
            .ToListAsync(ct);

        if (expired.Count > 0)
            _db.MetricCaches.RemoveRange(expired);
    }

    public Task AddAsync(ProfileMetricCacheEntity entity, CancellationToken ct)
        => _db.MetricCaches.AddAsync(entity, ct).AsTask();

    public void Update(ProfileMetricCacheEntity entity)
        => _db.MetricCaches.Update(entity);

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
