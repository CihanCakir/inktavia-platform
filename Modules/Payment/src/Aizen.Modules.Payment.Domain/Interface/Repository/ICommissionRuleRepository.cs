using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Commission;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

public interface ICommissionRuleRepository
{
    // ── Resolution (core engine, BE-P2) ───────────────────────────────────────

    /// <summary>
    /// Pure resolution (NO writes): resolves the single most-specific rule for <paramref name="ctx"/> via the
    /// 8-level specificity matrix + Priority tie-break (§13.7). Throws
    /// <c>CommissionRuleConflict</c> if the top tier has ≥2 rules tied on (specificity, priority). Returns the
    /// matched <see cref="CommissionResolution"/> (rate + ruleId + specificity + source) or null if no match.
    /// Applied-count is NOT touched here — use <see cref="MarkAppliedAsync"/> at acceptance.
    /// </summary>
    Task<CommissionResolution?> ResolveAsync(
        CommissionResolveContext ctx,
        DateTime atUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Backward-compatible thin wrapper over <see cref="ResolveAsync"/> for the legacy 3-dimension callers.
    /// Maps to a context and returns the matched rule's rate (or null). Performs NO writes.
    /// </summary>
    Task<decimal?> ResolveRateAsync(
        long?   providerProfileId,
        long?   providerPlanId,
        string? categoryCode,
        DateTime atUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Increments <c>ResolvedAppliedCount</c> on the given rule and saves — the ONLY place applied-count is
    /// bumped, called when a resolved rule is actually applied at acceptance (P8). Never during resolve/preview.
    /// Throws <c>CommissionRuleNotFound</c> if the rule does not exist.
    /// </summary>
    Task MarkAppliedAsync(long ruleId, CancellationToken ct = default);

    /// <summary>
    /// Create/Update conflict guard (§3): returns the first existing active rule that would form an overlap
    /// with <paramref name="candidate"/> (same scope-key + Priority + overlapping window), or null. Pass the
    /// candidate's own Id (0 for a new rule) so an update is not flagged against itself.
    /// </summary>
    Task<CommissionRuleEntity?> FindOverlappingActiveRuleAsync(
        CommissionRuleEntity candidate,
        CancellationToken ct = default);

    // ── Read ──────────────────────────────────────────────────────────────────

    Task<List<CommissionRuleEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the rules active at <paramref name="atUtc"/> (IsActive + effective window) — the same set
    /// <see cref="ResolveAsync"/> filters internally. Used by BE-S7 to resolve a whole line-set from one load.
    /// </summary>
    Task<List<CommissionRuleEntity>> GetActiveAtAsync(DateTime atUtc, CancellationToken ct = default);

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
