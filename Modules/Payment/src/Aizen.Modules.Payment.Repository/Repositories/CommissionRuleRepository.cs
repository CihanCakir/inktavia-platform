using Aizen.Core.Infrastructure.Exception;
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

    // ── Resolution (core engine, BE-P2) ───────────────────────────────────────

    /// <summary>
    /// Pure resolution: loads the active-at-<paramref name="atUtc"/> rule set and delegates to the domain
    /// <see cref="CommissionRuleResolver"/> for the 8-level specificity + Priority + fail-loud conflict logic.
    /// No writes.
    /// </summary>
    public async Task<CommissionResolution?> ResolveAsync(
        CommissionResolveContext ctx, DateTime atUtc, CancellationToken ct = default)
    {
        var activeRules = await _db.CommissionRules
            .AsNoTracking()
            .Where(x => x.IsActive && x.EffectiveFrom <= atUtc && (x.EffectiveTo == null || x.EffectiveTo >= atUtc))
            .ToListAsync(ct);

        return CommissionRuleResolver.Resolve(activeRules, ctx);
    }

    /// <summary>Backward-compatible 3-dimension wrapper — maps to a context and returns the rate. No writes.</summary>
    public async Task<decimal?> ResolveRateAsync(
        long? providerProfileId, long? providerPlanId,
        string? categoryCode, DateTime atUtc, CancellationToken ct)
    {
        var resolution = await ResolveAsync(
            new CommissionResolveContext(
                ProviderProfileId: providerProfileId,
                ProviderPlanId:    providerPlanId,
                CategoryCode:      categoryCode),
            atUtc, ct);

        return resolution?.Rate;
    }

    /// <summary>Applied-count bump — the only mutation of ResolvedAppliedCount. Acceptance-time only (P8).</summary>
    public async Task MarkAppliedAsync(long ruleId, CancellationToken ct = default)
    {
        var rule = await _db.CommissionRules.FirstOrDefaultAsync(x => x.Id == ruleId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CommissionRuleNotFound);

        rule.IncrementAppliedCount();
        _db.CommissionRules.Update(rule);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Create/Update conflict guard — delegates the overlap test to the pure domain resolver.</summary>
    public async Task<CommissionRuleEntity?> FindOverlappingActiveRuleAsync(
        CommissionRuleEntity candidate, CancellationToken ct = default)
    {
        var activeRules = await _db.CommissionRules
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(ct);

        return CommissionRuleResolver.FindOverlappingConflict(candidate, activeRules);
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    public Task<List<CommissionRuleEntity>> GetAllAsync(CancellationToken ct)
        => _db.CommissionRules.OrderBy(x => x.RuleType).ThenBy(x => x.Id).ToListAsync(ct);

    /// <summary>The active-at-instant set (same filter <see cref="ResolveAsync"/> uses) for BE-S7 line-set resolution.</summary>
    public Task<List<CommissionRuleEntity>> GetActiveAtAsync(DateTime atUtc, CancellationToken ct = default)
        => _db.CommissionRules
            .AsNoTracking()
            .Where(x => x.IsActive && x.EffectiveFrom <= atUtc && (x.EffectiveTo == null || x.EffectiveTo >= atUtc))
            .ToListAsync(ct);

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

    public Task<CommissionRuleEntity?> GetPublishedStandardGlobalRuleAsync(DateTime atUtc, CancellationToken ct = default)
        => _db.CommissionRules
            .AsNoTracking()
            .Where(x => x.RuleType == CommissionRuleType.Global
                        && x.Priority == CommissionRulePriority.Standard
                        && x.IsActive
                        && x.EffectiveFrom <= atUtc
                        && (x.EffectiveTo == null || atUtc < x.EffectiveTo))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

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
