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

    // ── Resolution (core engine) ──────────────────────────────────────────────

    public async Task<decimal?> ResolveRateAsync(
        long? providerProfileId, long? providerPlanId,
        string? categoryCode, DateTime atUtc, CancellationToken ct)
    {
        var activeRules = await _db.CommissionRules
            .Where(x => x.IsActive && x.EffectiveFrom <= atUtc && (x.EffectiveTo == null || x.EffectiveTo >= atUtc))
            .ToListAsync(ct);

        CommissionRuleEntity? matched = null;

        // 1. Provider override
        if (providerProfileId.HasValue)
            matched = activeRules
                .Where(x => x.RuleType == CommissionRuleType.ProviderOverride && x.ProviderProfileId == providerProfileId)
                .OrderByDescending(x => x.EffectiveFrom)
                .FirstOrDefault();

        // 2. Plan level
        if (matched == null && providerPlanId.HasValue)
            matched = activeRules.FirstOrDefault(x => x.RuleType == CommissionRuleType.Plan && x.ProviderPlanId == providerPlanId);

        // 3. Category level
        if (matched == null && !string.IsNullOrWhiteSpace(categoryCode))
            matched = activeRules.FirstOrDefault(x => x.RuleType == CommissionRuleType.Category &&
                string.Equals(x.CategoryCode, categoryCode, StringComparison.OrdinalIgnoreCase));

        // 4. Global default
        if (matched == null)
            matched = activeRules.FirstOrDefault(x => x.RuleType == CommissionRuleType.Global);

        if (matched == null) return null;

        // Increment applied counter
        matched.IncrementAppliedCount();
        _db.CommissionRules.Update(matched);
        await _db.SaveChangesAsync(ct);

        return matched.CommissionRate;
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    public Task<List<CommissionRuleEntity>> GetAllAsync(CancellationToken ct)
        => _db.CommissionRules.OrderBy(x => x.RuleType).ThenBy(x => x.Id).ToListAsync(ct);

    public async Task<(List<CommissionRuleEntity> Items, int Total)> GetPagedAsync(
        CommissionRuleType?     ruleType,
        CommissionRuleStatus?   status,
        CommissionRulePriority? priority,
        int                     skip,
        int                     take,
        TransactionContextType? contextType      = null,
        CommercialModel?        commercialModel  = null,
        string?                 productCode      = null,
        SalesChannel?           salesChannel     = null,
        string?                 search           = null,
        long?                   providerProfileId = null,
        DateTime?               effectiveOnUtc   = null,
        CancellationToken       ct               = default)
    {
        var q = _db.CommissionRules.AsQueryable();

        if (ruleType.HasValue)          q = q.Where(x => x.RuleType          == ruleType.Value);
        if (status.HasValue)            q = q.Where(x => x.Status            == status.Value);
        if (priority.HasValue)          q = q.Where(x => x.Priority          == priority.Value);
        if (contextType.HasValue)       q = q.Where(x => x.ContextType       == contextType.Value);
        if (commercialModel.HasValue)   q = q.Where(x => x.CommercialModel   == commercialModel.Value);
        if (salesChannel.HasValue)      q = q.Where(x => x.SalesChannel      == salesChannel.Value);
        if (providerProfileId.HasValue) q = q.Where(x => x.ProviderProfileId == providerProfileId.Value);

        if (!string.IsNullOrWhiteSpace(productCode))
            q = q.Where(x => x.ProductCode == productCode.ToUpperInvariant());

        if (effectiveOnUtc.HasValue)
            q = q.Where(x => x.EffectiveFrom <= effectiveOnUtc.Value &&
                              (x.EffectiveTo == null || x.EffectiveTo >= effectiveOnUtc.Value));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLowerInvariant();
            q = q.Where(x =>
                (x.RuleCode     != null && x.RuleCode.ToLower().Contains(s))    ||
                (x.RuleName     != null && x.RuleName.ToLower().Contains(s))    ||
                (x.CategoryCode != null && x.CategoryCode.ToLower().Contains(s)) ||
                (x.ProductCode  != null && x.ProductCode.ToLower().Contains(s)));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => (int)x.Priority)
            .ThenByDescending(x => x.EffectiveFrom)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<CommissionRuleEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.CommissionRules.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<CommissionRuleStatsResult> GetStatsAsync(CancellationToken ct)
    {
        var all = await _db.CommissionRules.ToListAsync(ct);
        var globalRate = all
            .Where(x => x.RuleType == CommissionRuleType.Global && x.IsActive)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefault()?.CommissionRate ?? 0m;

        return new CommissionRuleStatsResult(
            TotalRules:     all.Count,
            ActiveRules:    all.Count(x => x.Status == CommissionRuleStatus.Active),
            EmergencyRules: all.Count(x => x.Priority == CommissionRulePriority.EMERGENCY && x.Status == CommissionRuleStatus.Active),
            GlobalBaseRate: globalRate,
            ScheduledRules: all.Count(x => x.Status == CommissionRuleStatus.Scheduled),
            DraftRules:     all.Count(x => x.Status == CommissionRuleStatus.Draft)
        );
    }

    // ── Write ─────────────────────────────────────────────────────────────────

    public Task AddAsync(CommissionRuleEntity entity, CancellationToken ct)
        => _db.CommissionRules.AddAsync(entity, ct).AsTask();

    public void Update(CommissionRuleEntity entity) => _db.CommissionRules.Update(entity);
    public void Remove(CommissionRuleEntity entity) => _db.CommissionRules.Remove(entity);
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    // ── Code generation ───────────────────────────────────────────────────────

    public async Task<string> GenerateRuleCodeAsync(CancellationToken ct)
    {
        var year  = DateTime.UtcNow.Year;
        var count = await _db.CommissionRules.CountAsync(ct);
        var seq   = (count + 1).ToString("D3");
        // Generate a 3-char alpha suffix for uniqueness
        var suffix = ((char)('A' + (count % 26))).ToString()
                   + ((char)('A' + (count / 26 % 26))).ToString()
                   + (count % 10);
        return $"CR-{year}-{suffix}{seq}";
    }
}
