using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

public sealed class CommissionRuleRepository : ICommissionRuleRepository
{
    private readonly PaymentDbContext _db;
    public CommissionRuleRepository(PaymentDbContext db) => _db = db;

    public async Task<decimal?> ResolveRateAsync(
        long? providerProfileId, long? providerPlanId,
        string? categoryCode, DateTime atUtc, CancellationToken ct)
    {
        var activeRules = await _db.CommissionRules
            .Where(x => x.IsActive && x.EffectiveFrom <= atUtc && (x.EffectiveTo == null || x.EffectiveTo >= atUtc))
            .ToListAsync(ct);

        // 1. Provider override
        if (providerProfileId.HasValue)
        {
            var overrideRule = activeRules
                .Where(x => x.RuleType == CommissionRuleType.ProviderOverride && x.ProviderProfileId == providerProfileId)
                .OrderByDescending(x => x.EffectiveFrom)
                .FirstOrDefault();
            if (overrideRule != null) return overrideRule.CommissionRate;
        }

        // 2. Plan level
        if (providerPlanId.HasValue)
        {
            var planRule = activeRules
                .FirstOrDefault(x => x.RuleType == CommissionRuleType.Plan && x.ProviderPlanId == providerPlanId);
            if (planRule != null) return planRule.CommissionRate;
        }

        // 3. Category level
        if (!string.IsNullOrWhiteSpace(categoryCode))
        {
            var catRule = activeRules
                .FirstOrDefault(x => x.RuleType == CommissionRuleType.Category &&
                    string.Equals(x.CategoryCode, categoryCode, StringComparison.OrdinalIgnoreCase));
            if (catRule != null) return catRule.CommissionRate;
        }

        // 4. Global default
        var globalRule = activeRules.FirstOrDefault(x => x.RuleType == CommissionRuleType.Global);
        return globalRule?.CommissionRate;
    }

    public Task<List<CommissionRuleEntity>> GetAllAsync(CancellationToken ct)
        => _db.CommissionRules.OrderBy(x => x.RuleType).ThenBy(x => x.Id).ToListAsync(ct);

    public Task<CommissionRuleEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.CommissionRules.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task AddAsync(CommissionRuleEntity entity, CancellationToken ct)
        => _db.CommissionRules.AddAsync(entity, ct).AsTask();

    public void Update(CommissionRuleEntity entity) => _db.CommissionRules.Update(entity);
    public void Remove(CommissionRuleEntity entity) => _db.CommissionRules.Remove(entity);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
