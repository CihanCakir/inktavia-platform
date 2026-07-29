using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

/// <summary>
/// Persistence + pure resolution for <see cref="ProfitProtectionPolicyEntity"/> (BE-P5). Resolution loads the active
/// policy set and delegates the single-active / conflict logic to the domain resolver — no writes on resolve.
/// </summary>
public sealed class ProfitProtectionPolicyRepository : IProfitProtectionPolicyRepository
{
    private readonly PaymentDbContext _db;
    public ProfitProtectionPolicyRepository(PaymentDbContext db) => _db = db;

    public async Task<ProfitProtectionPolicyEntity?> ResolveAsync(string currency, DateTime atUtc, CancellationToken ct = default)
    {
        var cur = currency.ToUpperInvariant();
        var active = await _db.ProfitProtectionPolicies
            .AsNoTracking()
            .Where(x => x.CurrencyCode == cur && x.IsActive)
            .ToListAsync(ct);

        return ProfitProtectionPolicyResolver.Resolve(active, cur, atUtc);
    }

    public async Task<ProfitProtectionPolicyEntity?> FindOverlappingActivePolicyAsync(
        ProfitProtectionPolicyEntity candidate, CancellationToken ct = default)
    {
        var active = await _db.ProfitProtectionPolicies
            .AsNoTracking()
            .Where(x => x.CurrencyCode == candidate.CurrencyCode && x.IsActive)
            .ToListAsync(ct);

        return ProfitProtectionPolicyResolver.FindOverlappingConflict(candidate, active);
    }

    public Task<ProfitProtectionPolicyEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.ProfitProtectionPolicies.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<ProfitProtectionPolicyEntity>> GetAllAsync(CancellationToken ct = default)
        => _db.ProfitProtectionPolicies.OrderByDescending(x => x.EffectiveFrom).ToListAsync(ct);

    public Task AddAsync(ProfitProtectionPolicyEntity entity, CancellationToken ct = default)
        => _db.ProfitProtectionPolicies.AddAsync(entity, ct).AsTask();

    public void Update(ProfitProtectionPolicyEntity entity) => _db.ProfitProtectionPolicies.Update(entity);
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task<string> GenerateCodeAsync(CancellationToken ct = default)
    {
        var year  = DateTime.UtcNow.Year;
        var count = await _db.ProfitProtectionPolicies.CountAsync(ct);
        var seq   = (count + 1).ToString("D3");
        var suffix = ((char)('A' + (count % 26))).ToString();
        return $"PPOL-{year}-{suffix}{seq}";
    }
}

/// <summary>Insert-only persistence for <see cref="ProfitProtectionEvaluationLogEntity"/>.</summary>
public sealed class ProfitProtectionEvaluationLogRepository : IProfitProtectionEvaluationLogRepository
{
    private readonly PaymentDbContext _db;
    public ProfitProtectionEvaluationLogRepository(PaymentDbContext db) => _db = db;

    public Task AddAsync(ProfitProtectionEvaluationLogEntity entity, CancellationToken ct = default)
        => _db.ProfitProtectionEvaluationLogs.AddAsync(entity, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
