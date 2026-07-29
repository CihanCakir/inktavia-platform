using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

/// <summary>Persistence for <see cref="ProviderCommissionBenefitRuleEntity"/> (BE-P7). Resolution is pure (in the domain resolver).</summary>
public sealed class ProviderCommissionBenefitRuleRepository : IProviderCommissionBenefitRuleRepository
{
    private readonly PaymentDbContext _db;
    public ProviderCommissionBenefitRuleRepository(PaymentDbContext db) => _db = db;

    public Task<List<ProviderCommissionBenefitRuleEntity>> GetActiveCandidatesAsync(
        string currency, DateTime atUtc, CancellationToken ct = default)
    {
        var cur = currency.ToUpperInvariant();
        return _db.ProviderCommissionBenefitRules
            .AsNoTracking()
            .Where(x => x.CurrencyCode == cur && x.IsActive
                     && x.EffectiveFrom <= atUtc && (x.EffectiveTo == null || x.EffectiveTo > atUtc))
            .ToListAsync(ct);
    }

    public async Task<ProviderCommissionBenefitRuleEntity?> FindOverlappingActiveRuleAsync(
        ProviderCommissionBenefitRuleEntity candidate, CancellationToken ct = default)
    {
        var active = await _db.ProviderCommissionBenefitRules.AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
        return active.FirstOrDefault(e =>
            e.Id != candidate.Id
            && e.ProviderProfileId == candidate.ProviderProfileId
            && e.ProviderPlanId == candidate.ProviderPlanId
            && string.Equals(e.ApplicableCategoryCodesCsv ?? "", candidate.ApplicableCategoryCodesCsv ?? "", StringComparison.OrdinalIgnoreCase)
            && string.Equals(e.CurrencyCode, candidate.CurrencyCode, StringComparison.OrdinalIgnoreCase)
            && e.Priority == candidate.Priority
            && candidate.EffectiveFrom < (e.EffectiveTo ?? DateTime.MaxValue)
            && e.EffectiveFrom < (candidate.EffectiveTo ?? DateTime.MaxValue));
    }

    public Task<ProviderCommissionBenefitRuleEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.ProviderCommissionBenefitRules.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<ProviderCommissionBenefitRuleEntity>> GetAllAsync(CancellationToken ct = default)
        => _db.ProviderCommissionBenefitRules.OrderByDescending(x => x.EffectiveFrom).ToListAsync(ct);

    public Task AddAsync(ProviderCommissionBenefitRuleEntity entity, CancellationToken ct = default)
        => _db.ProviderCommissionBenefitRules.AddAsync(entity, ct).AsTask();

    public void Update(ProviderCommissionBenefitRuleEntity entity) => _db.ProviderCommissionBenefitRules.Update(entity);
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public Task<bool> ExistsByRuleCodeAsync(string ruleCode, CancellationToken ct = default)
        => _db.ProviderCommissionBenefitRules.AnyAsync(x => x.RuleCode == ruleCode, ct);

    public async Task<string> GenerateRuleCodeAsync(CancellationToken ct = default)
    {
        var year  = DateTime.UtcNow.Year;
        var count = await _db.ProviderCommissionBenefitRules.CountAsync(ct);
        var suffix = ((char)('A' + (count % 26))).ToString();
        return $"PCB-{year}-{suffix}{(count + 1):D3}";
    }
}

/// <summary>
/// Persistence for the entitlement + usage ledger (BE-P7). <see cref="SaveChangesConcurrencySafeAsync"/> maps an EF
/// optimistic-concurrency clash → <see cref="PaymentErrorCode.ProviderCommissionBenefitConcurrencyConflict"/>.
/// </summary>
public sealed class ProviderCommissionBenefitEntitlementRepository : IProviderCommissionBenefitEntitlementRepository
{
    private readonly PaymentDbContext _db;
    public ProviderCommissionBenefitEntitlementRepository(PaymentDbContext db) => _db = db;

    public Task<ProviderCommissionBenefitEntitlementEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.ProviderCommissionBenefitEntitlements.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<ProviderCommissionBenefitEntitlementEntity?> GetActiveByProviderAndRuleAsync(
        long providerProfileId, long benefitRuleId, DateTime atUtc, CancellationToken ct = default)
        => _db.ProviderCommissionBenefitEntitlements.FirstOrDefaultAsync(x =>
               x.ProviderProfileId == providerProfileId && x.BenefitRuleId == benefitRuleId
               && x.Status == ProviderCommissionBenefitEntitlementStatus.Active
               && x.GrantedFrom <= atUtc && (x.GrantedTo == null || x.GrantedTo > atUtc), ct);

    public Task<ProviderCommissionBenefitUsageEntity?> GetUsageByIdAsync(long usageId, CancellationToken ct = default)
        => _db.ProviderCommissionBenefitUsages.FirstOrDefaultAsync(x => x.Id == usageId, ct);

    public Task<ProviderCommissionBenefitUsageEntity?> GetUsageByContextRefAsync(
        long entitlementId, string contextRef, CancellationToken ct = default)
        => _db.ProviderCommissionBenefitUsages.FirstOrDefaultAsync(x => x.EntitlementId == entitlementId && x.ContextRef == contextRef, ct);

    public Task AddEntitlementAsync(ProviderCommissionBenefitEntitlementEntity entity, CancellationToken ct = default)
        => _db.ProviderCommissionBenefitEntitlements.AddAsync(entity, ct).AsTask();

    public Task AddUsageAsync(ProviderCommissionBenefitUsageEntity usage, CancellationToken ct = default)
        => _db.ProviderCommissionBenefitUsages.AddAsync(usage, ct).AsTask();

    public void UpdateEntitlement(ProviderCommissionBenefitEntitlementEntity entity)
        => _db.ProviderCommissionBenefitEntitlements.Update(entity);

    public async Task SaveChangesConcurrencySafeAsync(CancellationToken ct = default)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AizenBusinessException(
                (int)PaymentErrorCode.ProviderCommissionBenefitConcurrencyConflict,
                "The entitlement was modified by another offer — retry after reloading (optimistic concurrency).");
        }
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task<string> GenerateEntitlementCodeAsync(CancellationToken ct = default)
    {
        var year  = DateTime.UtcNow.Year;
        var count = await _db.ProviderCommissionBenefitEntitlements.CountAsync(ct);
        var suffix = ((char)('A' + (count % 26))).ToString();
        return $"PCE-{year}-{suffix}{(count + 1):D3}";
    }
}
