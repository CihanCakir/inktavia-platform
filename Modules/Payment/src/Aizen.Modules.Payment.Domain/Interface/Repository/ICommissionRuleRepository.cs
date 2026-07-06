using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Commission;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

public interface ICommissionRuleRepository
{
    // ── Resolution (core engine) ──────────────────────────────────────────────

    /// <summary>
    /// Resolves the effective commission rate using precedence:
    /// ProviderOverride > Plan > Category > Global.
    /// Also increments ResolvedAppliedCount on the matched rule.
    /// Returns the rate (e.g. 0.12 for 12%) or null if no rule found.
    /// </summary>
    Task<decimal?> ResolveRateAsync(
        long?   providerProfileId,
        long?   providerPlanId,
        string? categoryCode,
        DateTime atUtc,
        CancellationToken ct = default);

    // ── Read ──────────────────────────────────────────────────────────────────

    Task<List<CommissionRuleEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a paged list of commission rules with optional filters.
    /// Phase 13 (July 2026): Extended with CargoDry targeting + labeling + date + search filters.
    /// </summary>
    Task<(List<CommissionRuleEntity> Items, int Total)> GetPagedAsync(
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
        CancellationToken       ct               = default);

    Task<CommissionRuleEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<CommissionRuleStatsResult> GetStatsAsync(CancellationToken ct = default);

    // ── Write ─────────────────────────────────────────────────────────────────

    Task AddAsync(CommissionRuleEntity entity, CancellationToken ct = default);
    void Update(CommissionRuleEntity entity);
    void Remove(CommissionRuleEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);

    // ── Code generation ───────────────────────────────────────────────────────

    /// <summary>Generates the next rule code in "CR-YYYY-XXX" format.</summary>
    Task<string> GenerateRuleCodeAsync(CancellationToken ct = default);
}

/// <summary>KPI counts returned by GetStatsAsync for the admin commission dashboard strip.</summary>
public sealed record CommissionRuleStatsResult(
    int     TotalRules,
    int     ActiveRules,
    int     EmergencyRules,
    decimal GlobalBaseRate,
    int     ScheduledRules,
    int     DraftRules
);
