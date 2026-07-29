using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

/// <summary>
/// Persistence + pure resolution for <see cref="PlatformFeeRuleEntity"/> (BE-P3). Resolution loads the active
/// rule set and delegates the specificity/conflict logic to the domain <see cref="PlatformFeeRuleResolver"/> —
/// no writes on the resolve path.
/// </summary>
public sealed class PlatformFeeRuleRepository : IPlatformFeeRuleRepository
{
    private readonly PaymentDbContext _db;
    public PlatformFeeRuleRepository(PaymentDbContext db) => _db = db;

    // ── Resolution (pure) ───────────────────────────────────────────────────────

    public async Task<PlatformFeeResolution?> ResolveAsync(
        PlatformFeeResolveContext ctx, DateTime atUtc, CancellationToken ct = default)
    {
        var activeRules = await _db.PlatformFeeRules
            .AsNoTracking()
            .Where(x => x.IsActive && x.EffectiveFrom <= atUtc && (x.EffectiveTo == null || x.EffectiveTo >= atUtc))
            .ToListAsync(ct);

        return PlatformFeeRuleResolver.Resolve(activeRules, ctx);
    }

    public async Task<PlatformFeeRuleEntity?> FindOverlappingActiveRuleAsync(
        PlatformFeeRuleEntity candidate, CancellationToken ct = default)
    {
        var activeRules = await _db.PlatformFeeRules
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(ct);

        return PlatformFeeRuleResolver.FindOverlappingConflict(candidate, activeRules);
    }

    // ── Read ────────────────────────────────────────────────────────────────────

    public Task<List<PlatformFeeRuleEntity>> GetAllAsync(CancellationToken ct = default)
        => _db.PlatformFeeRules.OrderBy(x => x.Id).ToListAsync(ct);

    public Task<PlatformFeeRuleEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.PlatformFeeRules.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<(List<PlatformFeeRuleEntity> Items, int Total)> GetPagedAsync(
        PlatformFeeModel?     model,
        CommissionRuleStatus? status,
        string?               currencyCode,
        string?               categoryCode,
        string?               customerType,
        int                   skip,
        int                   take,
        CancellationToken     ct = default)
    {
        var q = _db.PlatformFeeRules.AsQueryable();

        if (model.HasValue)   q = q.Where(x => x.Model  == model.Value);
        if (status.HasValue)  q = q.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(currencyCode)) q = q.Where(x => x.CurrencyCode == currencyCode.ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(categoryCode)) q = q.Where(x => x.CategoryCode == categoryCode.ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(customerType)) q = q.Where(x => x.CustomerType == customerType.ToUpperInvariant());

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => (int)x.Priority)
            .ThenByDescending(x => x.EffectiveFrom)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    // ── Write ─────────────────────────────────────────────────────────────────

    public Task AddAsync(PlatformFeeRuleEntity entity, CancellationToken ct = default)
        => _db.PlatformFeeRules.AddAsync(entity, ct).AsTask();

    public void Update(PlatformFeeRuleEntity entity) => _db.PlatformFeeRules.Update(entity);
    public void Remove(PlatformFeeRuleEntity entity) => _db.PlatformFeeRules.Remove(entity);
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    // ── Code generation ───────────────────────────────────────────────────────

    public async Task<string> GenerateRuleCodeAsync(CancellationToken ct = default)
    {
        var year  = DateTime.UtcNow.Year;
        var count = await _db.PlatformFeeRules.CountAsync(ct);
        var seq   = (count + 1).ToString("D3");
        var suffix = ((char)('A' + (count % 26))).ToString()
                   + ((char)('A' + (count / 26 % 26))).ToString();
        return $"PFR-{year}-{suffix}{seq}";
    }
}
