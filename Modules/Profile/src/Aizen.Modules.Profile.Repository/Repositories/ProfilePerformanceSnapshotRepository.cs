using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Domain.Entities.Performance;
using Aizen.Modules.Profile.Domain.Interface.Repository;
using Aizen.Modules.Profile.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Profile.Repository.Repositories;

public sealed class ProfilePerformanceSnapshotRepository : IProfilePerformanceSnapshotRepository
{
    private readonly ProfileDbContext _db;
    public ProfilePerformanceSnapshotRepository(ProfileDbContext db) => _db = db;

    public Task<ProfilePerformanceSnapshotEntity?> GetByProfileAsync(
        long profileId, ProfileType profileType, CancellationToken ct)
        => _db.PerformanceSnapshots
              .FirstOrDefaultAsync(x => x.ProfileId == profileId && x.ProfileType == profileType, ct);

    public Task<List<ProfilePerformanceSnapshotEntity>> GetByTierAsync(
        PriorityTier tier, int skip, int take, CancellationToken ct)
        => _db.PerformanceSnapshots
              .Where(x => x.PriorityTier == tier && x.IsActive)
              .OrderByDescending(x => x.OverallScore)
              .Skip(skip).Take(take)
              .ToListAsync(ct);

    public Task<List<ProfilePerformanceSnapshotEntity>> GetWithActiveRiskSignalsAsync(
        int skip, int take, CancellationToken ct)
        => _db.PerformanceSnapshots
              .Where(x => x.HasActiveRiskSignal && x.IsActive)
              .OrderByDescending(x => x.ActiveRiskSignalMaxSeverity)
              .Skip(skip).Take(take)
              .ToListAsync(ct);

    public Task<int> CountByTierAsync(PriorityTier tier, CancellationToken ct)
        => _db.PerformanceSnapshots.CountAsync(x => x.PriorityTier == tier && x.IsActive, ct);

    public Task<List<ProfilePerformanceSnapshotEntity>> GetProviderProfilesRequiringRecomputeAsync(
        int staleThresholdHours, int batchSize, CancellationToken ct)
    {
        var threshold = DateTime.UtcNow.AddHours(-staleThresholdHours);
        return _db.PerformanceSnapshots
                  .Where(x => x.IsActive
                           && x.ProfileType == ProfileType.Provider
                           && (x.LastCalculatedAtUtc == null || x.LastCalculatedAtUtc < threshold))
                  .OrderBy(x => x.LastCalculatedAtUtc)
                  .Take(batchSize)
                  .ToListAsync(ct);
    }

    /// <summary>
    /// Phase 21: Bulk fetch for priority preview — returns active snapshots matching the supplied IDs + type.
    /// Profiles without a snapshot are silently omitted.
    /// </summary>
    public Task<List<ProfilePerformanceSnapshotEntity>> GetByProfilesAsync(
        IEnumerable<long> profileIds, ProfileType profileType, CancellationToken ct)
    {
        var ids = profileIds.ToList();
        return _db.PerformanceSnapshots
                  .Where(x => ids.Contains(x.ProfileId) && x.ProfileType == profileType && x.IsActive)
                  .ToListAsync(ct);
    }

    public Task AddAsync(ProfilePerformanceSnapshotEntity entity, CancellationToken ct)
        => _db.PerformanceSnapshots.AddAsync(entity, ct).AsTask();

    public void Update(ProfilePerformanceSnapshotEntity entity)
        => _db.PerformanceSnapshots.Update(entity);

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
