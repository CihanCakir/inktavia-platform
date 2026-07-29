using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Entities.Subscription;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

public interface IProviderPlanPriceRepository
{
    // ── Resolution (pure, BE-P4) ───────────────────────────────────────────────

    /// <summary>
    /// Pure point-in-time resolution (NO writes): the single Active price for (plan, currency, billing period)
    /// covering <paramref name="atUtc"/> on [EffectiveFrom, EffectiveTo). Throws ProviderPlanPriceConflict on an
    /// overlap (&gt;1). Returns null if none matches (caller maps to gap / not-found).
    /// </summary>
    Task<ProviderPlanPriceEntity?> ResolveAsync(
        long planId, string currency, BillingPeriod billing, DateTime atUtc, CancellationToken ct = default);

    /// <summary>
    /// Convenience: resolves the price active at a subscription's renewal instant (§13.2) so a renewal charge
    /// uses the price active at renewal, not the original snapshot. Pure.
    /// </summary>
    Task<ProviderPlanPriceEntity?> ResolveRenewalPriceAsync(
        ProviderPlanSubscriptionEntity subscription, DateTime atRenewalUtc, CancellationToken ct = default);

    /// <summary>
    /// Create/Update guard (§4): validates that adding/replacing <paramref name="candidate"/> keeps the scope's
    /// price chain non-overlapping AND gap-free. Returns the guard outcome.
    /// </summary>
    Task<PlanPriceGuardResult> ValidateInsertableAsync(
        ProviderPlanPriceEntity candidate, CancellationToken ct = default);

    // ── Read ────────────────────────────────────────────────────────────────────

    Task<ProviderPlanPriceEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<List<ProviderPlanPriceEntity>> GetByPlanAsync(long planId, CancellationToken ct = default);

    // ── Write ─────────────────────────────────────────────────────────────────

    Task AddAsync(ProviderPlanPriceEntity entity, CancellationToken ct = default);
    void Update(ProviderPlanPriceEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>True if a same-scope price with the same [EffectiveFrom, EffectiveTo) already exists (seed idempotency).</summary>
    Task<bool> ExistsSameRangeAsync(
        long planId, string currency, BillingPeriod billing, DateTime effectiveFrom, DateTime? effectiveTo,
        CancellationToken ct = default);

    /// <summary>Generates the next price code in "PPP-YYYY-XXX" format.</summary>
    Task<string> GenerateCodeAsync(CancellationToken ct = default);
}
