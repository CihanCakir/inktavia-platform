using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Domain.Entities.Performance;
using Aizen.Modules.Profile.Domain.Interface.Repository;
using Aizen.Modules.Profile.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Profile.Repository.Repositories;

/// <summary>Append-only implementation — no Update or Delete exposed.</summary>
public sealed class ProfileScoreHistoryRepository : IProfileScoreHistoryRepository
{
    private readonly ProfileDbContext _db;
    public ProfileScoreHistoryRepository(ProfileDbContext db) => _db = db;

    public Task<List<ProfileScoreHistoryEntity>> GetByProfileAsync(
        long profileId, ProfileType profileType, int skip, int take, CancellationToken ct)
        => _db.ScoreHistory
              .Where(x => x.ProfileId == profileId && x.ProfileType == profileType)
              .OrderByDescending(x => x.RecordedAtUtc)
              .Skip(skip).Take(take)
              .ToListAsync(ct);

    public Task<int> CountByProfileAsync(long profileId, ProfileType profileType, CancellationToken ct)
        => _db.ScoreHistory.CountAsync(x => x.ProfileId == profileId && x.ProfileType == profileType, ct);

    public Task AddAsync(ProfileScoreHistoryEntity entity, CancellationToken ct)
        => _db.ScoreHistory.AddAsync(entity, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
