using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDryProviderMilestoneAwardRepository : ICargoDryProviderMilestoneAwardRepository
{
    private readonly CargoDryDbContext _db;
    public CargoDryProviderMilestoneAwardRepository(CargoDryDbContext db) => _db = db;

    public Task<bool> ExistsAsync(long providerProfileId, string milestoneType, string periodKey, CancellationToken ct)
        => _db.ProviderMilestoneAwards.AnyAsync(
            x => x.ProviderProfileId == providerProfileId
              && x.MilestoneType == milestoneType
              && x.PeriodKey == periodKey, ct);

    public async Task AddAsync(CargoDryProviderMilestoneAwardEntity entity, CancellationToken ct)
        => await _db.ProviderMilestoneAwards.AddAsync(entity, ct);

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
