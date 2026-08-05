using Aizen.Modules.Payment.Domain.Entities.PartCommercialTerm;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

/// <summary>BE-S5 — persistence for part commercial terms. The effective-date/scope filtering is SQL; ranking + conflict is the pure resolver.</summary>
public sealed class PartCommercialTermRepository : IPartCommercialTermRepository
{
    private readonly PaymentDbContext _db;
    public PartCommercialTermRepository(PaymentDbContext db) => _db = db;

    public async Task<List<PartCommercialTermEntity>> GetActiveAtAsync(string currencyCode, DateTime atUtc, CancellationToken ct = default)
    {
        var cur = currencyCode.ToUpperInvariant();
        return await _db.PartCommercialTerms
            .AsNoTracking()
            .Where(x => x.CurrencyCode == cur && x.IsActive
                     && x.EffectiveFrom <= atUtc && (x.EffectiveTo == null || x.EffectiveTo > atUtc))
            .ToListAsync(ct);
    }

    public async Task<PartCommercialTermEntity?> FindOverlappingActiveTermAsync(PartCommercialTermEntity candidate, CancellationToken ct = default)
    {
        var active = await _db.PartCommercialTerms.AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
        return PartCommercialTermResolver.FindOverlappingConflict(candidate, active);
    }

    public async Task<int> GetMaxVersionForScopeAsync(PartCommercialTermEntity candidate, CancellationToken ct = default)
    {
        var all = await _db.PartCommercialTerms.AsNoTracking().ToListAsync(ct);
        var sameScope = all.Where(e =>
            EqualsCI(e.Brand, candidate.Brand)
            && EqualsCI(e.ProductCode, candidate.ProductCode)
            && e.ProviderProfileId == candidate.ProviderProfileId
            && EqualsCI(e.CategoryCode, candidate.CategoryCode)
            && EqualsCI(e.CurrencyCode, candidate.CurrencyCode)).ToList();
        return sameScope.Count == 0 ? 0 : sameScope.Max(e => e.Version);
    }

    public Task<PartCommercialTermEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.PartCommercialTerms.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<PartCommercialTermEntity>> GetAllAsync(CancellationToken ct = default)
        => _db.PartCommercialTerms.OrderByDescending(x => x.EffectiveFrom).ToListAsync(ct);

    public Task AddAsync(PartCommercialTermEntity entity, CancellationToken ct = default)
        => _db.PartCommercialTerms.AddAsync(entity, ct).AsTask();

    public void Update(PartCommercialTermEntity entity) => _db.PartCommercialTerms.Update(entity);
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task<string> GenerateCodeAsync(CancellationToken ct = default)
    {
        var year  = DateTime.UtcNow.Year;
        var count = await _db.PartCommercialTerms.CountAsync(ct);
        var suffix = ((char)('A' + (count % 26))).ToString();
        return $"PCT-{year}-{suffix}{(count + 1):D3}";
    }

    private static bool EqualsCI(string? x, string? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;
        return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
    }
}
