using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Commission;

/// <summary>
/// The request context a commission rule is resolved against (BE-P2, §13.7 / §20.11). Every field is
/// optional; a rule that declares a non-null scope dimension is a candidate only when the context supplies
/// the same value. Callers building line-level economics (ServiceRequest S7) pass the per-line dims.
/// </summary>
public sealed record CommissionResolveContext(
    long?                   ProviderProfileId     = null,
    long?                   ProviderPlanId        = null,
    string?                 CategoryCode          = null,
    string?                 ProductCode           = null,
    LineType?               LineType              = null,
    TransactionContextType? ContextType           = null,
    CommercialModel?        CommercialModel       = null,
    SalesChannel?           SalesChannel          = null,
    string?                 CurrencyCode          = null,
    CommissionEligibility?  CommissionEligibility = null);

/// <summary>
/// The matched-rule result of a resolution (§4). Carries enough to snapshot and audit the decision:
/// the winning rule's id/code/type, its rate, the computed <see cref="SpecificityRank"/>, the tie-break
/// <see cref="Priority"/>, and the <see cref="Source"/> (matched rule's RuleType — NOT request-inferred).
/// </summary>
public sealed record CommissionResolution(
    long                   RuleId,
    string                 RuleCode,
    CommissionRuleType     RuleType,
    decimal                Rate,
    int                    SpecificityRank,
    CommissionRulePriority Priority,
    string                 Source);

/// <summary>
/// Pure, side-effect-free commission resolution + conflict detection (§13.7). Kept in the domain (no DB)
/// so the 8-level specificity matrix, Priority tie-break and fail-loud conflict are fully unit-testable.
/// The repository feeds it the active rule set; it never writes.
/// </summary>
public static class CommissionRuleResolver
{
    /// <summary>
    /// Resolves the single most-specific rule for <paramref name="ctx"/> from <paramref name="activeRules"/>
    /// (already filtered to IsActive + effective at the resolve time). Selection: highest SpecificityRank →
    /// highest Priority. If ≥2 candidates tie on (SpecificityRank, Priority) → throws
    /// <see cref="AizenBusinessException"/>(<see cref="PaymentErrorCode.CommissionRuleConflict"/>) — never a
    /// silent FirstOrDefault. Returns null when nothing matches (caller decides fallback / not-found).
    /// </summary>
    public static CommissionResolution? Resolve(
        IEnumerable<CommissionRuleEntity> activeRules,
        CommissionResolveContext ctx)
    {
        var candidates = activeRules
            .Where(r => IsCandidate(r, ctx))
            .Select(r => (rule: r, rank: ComputeSpecificityRank(r)))
            .ToList();

        if (candidates.Count == 0)
            return null;

        var top = candidates
            .OrderByDescending(x => x.rank)
            .ThenByDescending(x => (int)x.rule.Priority)
            .First();

        var tied = candidates
            .Where(x => x.rank == top.rank && x.rule.Priority == top.rule.Priority)
            .ToList();

        if (tied.Count > 1)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.CommissionRuleConflict,
                $"Commission rule conflict: {tied.Count} active rules tie at specificity {top.rank} / " +
                $"priority {top.rule.Priority}. Rule ids: [{string.Join(", ", tied.Select(t => t.rule.Id))}]. " +
                "Resolution refuses to guess — deactivate or re-prioritise one of them.");

        var r = top.rule;
        return new CommissionResolution(
            RuleId:          r.Id,
            RuleCode:        r.RuleCode ?? string.Empty,
            RuleType:        r.RuleType,
            Rate:            r.CommissionRate,
            SpecificityRank: top.rank,
            Priority:        r.Priority,
            Source:          r.RuleType.ToString());
    }

    /// <summary>
    /// Create/Update-time guard (§3, primary): returns the first existing rule that would form an active
    /// overlap with <paramref name="candidate"/> — same scope-key (RuleType + all declared dims) AND same
    /// Priority AND overlapping [EffectiveFrom, EffectiveTo) — or null if none. The candidate itself is
    /// excluded by Id so an update doesn't conflict with itself.
    /// </summary>
    public static CommissionRuleEntity? FindOverlappingConflict(
        CommissionRuleEntity candidate,
        IEnumerable<CommissionRuleEntity> existingActiveRules)
        => existingActiveRules.FirstOrDefault(e =>
               e.Id != candidate.Id
               && e.IsActive
               && SameScopeKey(candidate, e)
               && candidate.Priority == e.Priority
               && WindowsOverlap(candidate, e));

    // ── Specificity (§13.7) ────────────────────────────────────────────────────

    /// <summary>
    /// SpecificityRank = baseLevel×10 + finerDimCount. The base level encodes the 8-level primary matrix
    /// (Provider+Plan+Category=8 … Global=1); finer dims (ProductCode/LineType/ContextType/CommercialModel/
    /// SalesChannel/CurrencyCode/CommissionEligibility) add tie-break weight WITHIN a base level (max 7 &lt; 10,
    /// so finer dims can never lift a rule across base levels).
    /// </summary>
    public static int ComputeSpecificityRank(CommissionRuleEntity r)
    {
        var hasProvider = r.ProviderProfileId.HasValue;
        var hasPlan     = r.ProviderPlanId.HasValue;
        var hasCategory = !string.IsNullOrWhiteSpace(r.CategoryCode);

        var baseLevel = (hasProvider, hasPlan, hasCategory) switch
        {
            (true,  true,  true ) => 8,   // Provider+Plan+Category
            (true,  false, true ) => 7,   // Provider+Category
            (true,  true,  false) => 6,   // Provider+Plan
            (true,  false, false) => 5,   // Provider
            (false, true,  true ) => 4,   // Plan+Category
            (false, true,  false) => 3,   // Plan
            (false, false, true ) => 2,   // Category
            _                     => 1,   // Global
        };

        var finer =
            (r.ProductCode           is not null ? 1 : 0) +
            (r.LineType              is not null ? 1 : 0) +
            (r.ContextType           is not null ? 1 : 0) +
            (r.CommercialModel       is not null ? 1 : 0) +
            (r.SalesChannel          is not null ? 1 : 0) +
            (r.CurrencyCode          is not null ? 1 : 0) +
            (r.CommissionEligibility is not null ? 1 : 0);

        return baseLevel * 10 + finer;
    }

    // ── Candidacy ───────────────────────────────────────────────────────────────

    /// <summary>A rule is a candidate only if every non-null scope dim it declares matches the context.</summary>
    public static bool IsCandidate(CommissionRuleEntity r, CommissionResolveContext ctx)
    {
        // Primary matrix
        if (r.ProviderProfileId.HasValue && r.ProviderProfileId != ctx.ProviderProfileId) return false;
        if (r.ProviderPlanId.HasValue    && r.ProviderPlanId    != ctx.ProviderPlanId)    return false;
        if (!string.IsNullOrWhiteSpace(r.CategoryCode) && !EqualsCI(r.CategoryCode, ctx.CategoryCode)) return false;

        // Finer dims
        if (!string.IsNullOrWhiteSpace(r.ProductCode) && !EqualsCI(r.ProductCode, ctx.ProductCode)) return false;
        if (r.LineType.HasValue              && r.LineType              != ctx.LineType)              return false;
        if (r.ContextType.HasValue           && r.ContextType           != ctx.ContextType)           return false;
        if (r.CommercialModel.HasValue       && r.CommercialModel       != ctx.CommercialModel)       return false;
        if (r.SalesChannel.HasValue          && r.SalesChannel          != ctx.SalesChannel)          return false;
        if (!string.IsNullOrWhiteSpace(r.CurrencyCode) && !EqualsCI(r.CurrencyCode, ctx.CurrencyCode)) return false;
        if (r.CommissionEligibility.HasValue && r.CommissionEligibility != ctx.CommissionEligibility) return false;

        return true;
    }

    // ── Scope-key + overlap (conflict) ──────────────────────────────────────────

    private static bool SameScopeKey(CommissionRuleEntity a, CommissionRuleEntity b)
        => a.RuleType              == b.RuleType
        && a.ProviderProfileId     == b.ProviderProfileId
        && a.ProviderPlanId        == b.ProviderPlanId
        && EqualsCI(a.CategoryCode,  b.CategoryCode)
        && EqualsCI(a.ProductCode,   b.ProductCode)
        && a.LineType              == b.LineType
        && a.ContextType           == b.ContextType
        && a.CommercialModel       == b.CommercialModel
        && a.SalesChannel          == b.SalesChannel
        && EqualsCI(a.CurrencyCode,  b.CurrencyCode)
        && a.CommissionEligibility == b.CommissionEligibility;

    /// <summary>Half-open interval overlap of [from, to) with null <c>to</c> meaning +infinity.</summary>
    private static bool WindowsOverlap(CommissionRuleEntity a, CommissionRuleEntity b)
        => a.EffectiveFrom < (b.EffectiveTo ?? DateTime.MaxValue)
        && b.EffectiveFrom < (a.EffectiveTo ?? DateTime.MaxValue);

    private static bool EqualsCI(string? x, string? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;
        return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
    }
}
