using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Repositories;

public sealed class CargoDryOwnerPreferredProviderRepository : ICargoDryOwnerPreferredProviderRepository
{
    private readonly CargoDryDbContext _db;
    public CargoDryOwnerPreferredProviderRepository(CargoDryDbContext db) => _db = db;

    public Task<CargoDryOwnerPreferredProviderEntity?> GetByOwnerAsync(long ownerUserId, CancellationToken ct)
        => _db.OwnerPreferredProviders.FirstOrDefaultAsync(x => x.OwnerUserId == ownerUserId, ct);

    public async Task<HashSet<long>> GetOwnerIdsPreferringProviderAsync(long providerProfileId, CancellationToken ct)
    {
        var ownerIds = await _db.OwnerPreferredProviders
            .AsNoTracking()
            .Where(x => x.ProviderProfileId == providerProfileId)
            .Select(x => x.OwnerUserId)
            .ToListAsync(ct);
        return ownerIds.ToHashSet();
    }

    public async Task AddAsync(CargoDryOwnerPreferredProviderEntity entity, CancellationToken ct)
        => await _db.OwnerPreferredProviders.AddAsync(entity, ct);

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
