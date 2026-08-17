using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.PlatformFee;

/// <summary>
/// The request context a platform fee rule is resolved against (BE-P3, §3). CurrencyCode is required (rules are
/// currency-scoped); CategoryCode / CustomerType are optional finer dims. A rule that declares a non-null dim is a
/// candidate only when the context supplies the same value.
/// </summary>
public sealed record PlatformFeeResolveContext(
    string  CurrencyCode = "TRY",
    string? CategoryCode = null,
    string? CustomerType = null);

/// <summary>
/// The matched-rule result of a platform fee resolution (§3). Carries the model + parameters needed by
/// <see cref="PlatformFeeCalculator"/> plus audit fields (specificity rank, priority, source tier).
/// </summary>
public sealed record PlatformFeeResolution(
    long                   RuleId,
    string                 RuleCode,
    PlatformFeeModel       Model,
    decimal?               Rate,
    decimal?               MinAmount,
    decimal?               MaxAmount,
    decimal?               FixedAmount,
    decimal?               VatRate,
    int                    SpecificityRank,
    CommissionRulePriority Priority,
    string                 Source);

/// <summary>
/// Pure, side-effect-free platform fee resolution + conflict detection (§3, mirrors the BE-P2 commission engine).
/// Kept in the domain (no DB) so the specificity matrix, Priority tie-break and fail-loud conflict are fully
/// unit-testable. The repository feeds it the active rule set; it never writes.
/// </summary>
public static class PlatformFeeRuleResolver
{
    /// <summary>
    /// Resolves the single most-specific rule for <paramref name="ctx"/> from <paramref name="activeRules"/>
    /// (already filtered to IsActive + effective at the resolve time). Selection: highest SpecificityRank →
    /// highest Priority. If ≥2 candidates tie on (SpecificityRank, Priority) → throws
    /// <see cref="AizenBusinessException"/>(<see cref="PaymentErrorCode.PlatformFeeRuleConflict"/>) — never a
    /// silent FirstOrDefault. Returns null when nothing matches (caller decides fallback / not-found).
    /// </summary>
    public static PlatformFeeResolution? Resolve(
        IEnumerable<PlatformFeeRuleEntity> activeRules,
        PlatformFeeResolveContext ctx)
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
                (int)PaymentErrorCode.PlatformFeeRuleConflict,
                $"Platform fee rule conflict: {tied.Count} active rules tie at specificity {top.rank} / " +
                $"priority {top.rule.Priority}. Rule ids: [{string.Join(", ", tied.Select(t => t.rule.Id))}]. " +
                "Resolution refuses to guess — deactivate or re-prioritise one of them.");

        var r = top.rule;
        return new PlatformFeeResolution(
            RuleId:          r.Id,
            RuleCode:        r.RuleCode ?? string.Empty,
            Model:           r.Model,
            Rate:            r.Rate,
            MinAmount:       r.MinAmount,
            MaxAmount:       r.MaxAmount,
            FixedAmount:     r.FixedAmount,
            VatRate:         r.VatRate,
            SpecificityRank: top.rank,
            Priority:        r.Priority,
            Source:          SourceLabel(r));
    }

    /// <summary>
    /// Create/Update-time guard (§7, mirrors BE-P2): returns the first existing active rule that would form an
    /// overlap with <paramref name="candidate"/> — same scope-key (CurrencyCode + CategoryCode + CustomerType)
    /// AND same Priority AND overlapping [EffectiveFrom, EffectiveTo) — or null. Candidate excluded by Id.
    /// </summary>
    public static PlatformFeeRuleEntity? FindOverlappingConflict(
        PlatformFeeRuleEntity candidate,
        IEnumerable<PlatformFeeRuleEntity> existingActiveRules)
        => existingActiveRules.FirstOrDefault(e =>
               e.Id != candidate.Id
               && e.IsActive
               && SameScopeKey(candidate, e)
               && candidate.Priority == e.Priority
               && WindowsOverlap(candidate, e));

    // ── Specificity (§3) ────────────────────────────────────────────────────────

    /// <summary>CustomerType+Category=4 &gt; CustomerType=3 &gt; Category=2 &gt; Global=1.</summary>
    public static int ComputeSpecificityRank(PlatformFeeRuleEntity r)
    {
        var hasCustomer = !string.IsNullOrWhiteSpace(r.CustomerType);
        var hasCategory = !string.IsNullOrWhiteSpace(r.CategoryCode);

        return (hasCustomer, hasCategory) switch
        {
            (true,  true ) => 4,
            (true,  false) => 3,
            (false, true ) => 2,
            _              => 1,
        };
    }

    // ── Candidacy ───────────────────────────────────────────────────────────────

    public static bool IsCandidate(PlatformFeeRuleEntity r, PlatformFeeResolveContext ctx)
    {
        if (!EqualsCI(r.CurrencyCode, ctx.CurrencyCode)) return false;   // currency is a required match
        if (!string.IsNullOrWhiteSpace(r.CategoryCode) && !EqualsCI(r.CategoryCode, ctx.CategoryCode)) return false;
        if (!string.IsNullOrWhiteSpace(r.CustomerType) && !EqualsCI(r.CustomerType, ctx.CustomerType)) return false;
        return true;
    }

    // ── Scope-key + overlap (conflict) ──────────────────────────────────────────

    private static bool SameScopeKey(PlatformFeeRuleEntity a, PlatformFeeRuleEntity b)
        => EqualsCI(a.CurrencyCode, b.CurrencyCode)
        && EqualsCI(a.CategoryCode, b.CategoryCode)
        && EqualsCI(a.CustomerType, b.CustomerType);

    private static bool WindowsOverlap(PlatformFeeRuleEntity a, PlatformFeeRuleEntity b)
        => a.EffectiveFrom < (b.EffectiveTo ?? DateTime.MaxValue)
        && b.EffectiveFrom < (a.EffectiveTo ?? DateTime.MaxValue);

    private static string SourceLabel(PlatformFeeRuleEntity r)
    {
        var hasCustomer = !string.IsNullOrWhiteSpace(r.CustomerType);
        var hasCategory = !string.IsNullOrWhiteSpace(r.CategoryCode);
        return (hasCustomer, hasCategory) switch
        {
            (true,  true ) => "CustomerType+Category",
            (true,  false) => "CustomerType",
            (false, true ) => "Category",
            _              => "Global",
        };
    }

    private static bool EqualsCI(string? x, string? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;
        return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
    }
}
