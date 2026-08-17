using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;

/// <summary>Resolve context: the customer's plan + the offer category + currency.</summary>
public sealed record CustomerDiscountResolveContext(
    long?   CustomerPlanId = null,
    string? CategoryCode   = null,
    string  CurrencyCode   = "TRY");

/// <summary>
/// Pure customer-discount resolution + conflict guard (§19.6, mirrors BE-P2). Specificity
/// <c>CustomerPlan+Category(4) &gt; CustomerPlan(3) &gt; Category(2) &gt; Global(1)</c>, then Priority; a tie on
/// (specificity, priority) among ≥2 active rules → fail-loud <see cref="PaymentErrorCode.CustomerDiscountRuleConflict"/>.
/// No DB, fully unit-testable.
/// </summary>
public static class CustomerDiscountRuleResolver
{
    public static CustomerDiscountRuleEntity? Resolve(
        IEnumerable<CustomerDiscountRuleEntity> scopedActiveRules, CustomerDiscountResolveContext ctx)
    {
        var candidates = scopedActiveRules
            .Where(r => IsCandidate(r, ctx))
            .Select(r => (rule: r, rank: ComputeSpecificityRank(r)))
            .ToList();

        if (candidates.Count == 0) return null;

        var top = candidates
            .OrderByDescending(x => x.rank)
            .ThenByDescending(x => (int)x.rule.Priority)
            .First();

        var tied = candidates.Where(x => x.rank == top.rank && x.rule.Priority == top.rule.Priority).ToList();
        if (tied.Count > 1)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.CustomerDiscountRuleConflict,
                $"Customer discount rule conflict: {tied.Count} active rules tie at specificity {top.rank} / " +
                $"priority {top.rule.Priority}. Rule ids: [{string.Join(", ", tied.Select(t => t.rule.Id))}].");

        return top.rule;
    }

    public static CustomerDiscountRuleEntity? FindOverlappingConflict(
        CustomerDiscountRuleEntity candidate, IEnumerable<CustomerDiscountRuleEntity> existingActive)
        => existingActive.FirstOrDefault(e =>
               e.Id != candidate.Id
               && e.IsActive
               && e.CustomerPlanId == candidate.CustomerPlanId
               && EqualsCI(e.CategoryCode, candidate.CategoryCode)
               && EqualsCI(e.CurrencyCode, candidate.CurrencyCode)
               && e.Priority == candidate.Priority
               && candidate.EffectiveFrom < (e.EffectiveTo ?? DateTime.MaxValue)
               && e.EffectiveFrom < (candidate.EffectiveTo ?? DateTime.MaxValue));

    public static int ComputeSpecificityRank(CustomerDiscountRuleEntity r)
    {
        var hasPlan     = r.CustomerPlanId.HasValue;
        var hasCategory = !string.IsNullOrWhiteSpace(r.CategoryCode);
        return (hasPlan, hasCategory) switch
        {
            (true,  true ) => 4,
            (true,  false) => 3,
            (false, true ) => 2,
            _              => 1,
        };
    }

    public static bool IsCandidate(CustomerDiscountRuleEntity r, CustomerDiscountResolveContext ctx)
    {
        if (!EqualsCI(r.CurrencyCode, ctx.CurrencyCode)) return false;                     // currency required
        if (r.CustomerPlanId.HasValue && r.CustomerPlanId != ctx.CustomerPlanId) return false;
        if (!string.IsNullOrWhiteSpace(r.CategoryCode) && !EqualsCI(r.CategoryCode, ctx.CategoryCode)) return false;
        return true;
    }

    private static bool EqualsCI(string? x, string? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;
        return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
    }
}
