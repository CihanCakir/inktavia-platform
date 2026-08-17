using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Domain.Entities.Performance;
using Aizen.Modules.Profile.Domain.Interface.Repository;
using Aizen.Modules.Profile.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Profile.Repository.Repositories;

public sealed class ProfileRiskSignalRepository : IProfileRiskSignalRepository
{
    private readonly ProfileDbContext _db;
    public ProfileRiskSignalRepository(ProfileDbContext db) => _db = db;

    public Task<ProfileRiskSignalEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.RiskSignals.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<ProfileRiskSignalEntity>> GetActiveByProfileAsync(
        long profileId, ProfileType profileType, CancellationToken ct)
        => _db.RiskSignals
              .Where(x => x.ProfileId == profileId && x.ProfileType == profileType && !x.IsResolved)
              .OrderByDescending(x => x.Severity)
              .ToListAsync(ct);

    public Task<List<ProfileRiskSignalEntity>> GetByProfileAsync(
        long profileId, ProfileType profileType, int skip, int take, CancellationToken ct)
        => _db.RiskSignals
              .Where(x => x.ProfileId == profileId && x.ProfileType == profileType)
              .OrderByDescending(x => x.DetectedAtUtc)
              .Skip(skip).Take(take)
              .ToListAsync(ct);

    public Task<bool> HasActiveSignalAboveAsync(
        long profileId, ProfileType profileType, RiskSignalSeverity minSeverity, CancellationToken ct)
        => _db.RiskSignals
              .AnyAsync(x => x.ProfileId == profileId
                          && x.ProfileType == profileType
                          && !x.IsResolved
                          && x.Severity >= minSeverity, ct);

    public async Task<RiskSignalSeverity?> GetMaxActiveSeverityAsync(
        long profileId, ProfileType profileType, CancellationToken ct)
    {
        var severities = await _db.RiskSignals
            .Where(x => x.ProfileId == profileId && x.ProfileType == profileType && !x.IsResolved)
            .Select(x => x.Severity)
            .ToListAsync(ct);

        return severities.Count == 0 ? null : severities.Max();
    }

    public Task AddAsync(ProfileRiskSignalEntity entity, CancellationToken ct)
        => _db.RiskSignals.AddAsync(entity, ct).AsTask();

    public void Update(ProfileRiskSignalEntity entity)
        => _db.RiskSignals.Update(entity);

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
