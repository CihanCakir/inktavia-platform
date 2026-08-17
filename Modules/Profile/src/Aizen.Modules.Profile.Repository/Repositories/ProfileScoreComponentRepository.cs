using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Domain.Entities.Performance;
using Aizen.Modules.Profile.Domain.Interface.Repository;
using Aizen.Modules.Profile.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Profile.Repository.Repositories;

public sealed class ProfileScoreComponentRepository : IProfileScoreComponentRepository
{
    private readonly ProfileDbContext _db;
    public ProfileScoreComponentRepository(ProfileDbContext db) => _db = db;

    public Task<List<ProfileScoreComponentEntity>> GetBySnapshotIdAsync(long snapshotId, CancellationToken ct)
        => _db.ScoreComponents
              .Where(x => x.SnapshotId == snapshotId)
              .OrderBy(x => x.Category)
              .ToListAsync(ct);

    public Task<List<ProfileScoreComponentEntity>> GetByProfileAsync(
        long profileId, ProfileType profileType, CancellationToken ct)
        => _db.ScoreComponents
              .Where(x => x.ProfileId == profileId && x.ProfileType == profileType)
              .OrderBy(x => x.Category)
              .ToListAsync(ct);

    public async Task ReplaceForSnapshotAsync(
        long snapshotId,
        IEnumerable<ProfileScoreComponentEntity> components,
        CancellationToken ct)
    {
        var existing = await _db.ScoreComponents
            .Where(x => x.SnapshotId == snapshotId)
            .ToListAsync(ct);
        if (existing.Count > 0)
            _db.ScoreComponents.RemoveRange(existing);

        await _db.ScoreComponents.AddRangeAsync(components, ct);
    }

    public Task AddAsync(ProfileScoreComponentEntity entity, CancellationToken ct)
        => _db.ScoreComponents.AddAsync(entity, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
