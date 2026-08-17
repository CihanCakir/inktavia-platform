using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.PartCommercialTerm;

/// <summary>
/// BE-S5b — the scope input to <see cref="PartCommercialTermResolver.Resolve"/> for one part line. Currency is a hard filter;
/// the four nullable dims drive specificity (product &gt; provider &gt; brand &gt; category &gt; global).
/// </summary>
public sealed record PartCommercialTermResolveContext(
    string? ProductCode       = null,
    long?   ProviderProfileId = null,
    string? Brand             = null,
    string? CategoryCode      = null,
    string  CurrencyCode      = "TRY");

/// <summary>
/// BE-S5b — pure, point-in-time scoped resolver for part commercial terms. Mirrors the established Payment resolver pattern
/// (CommissionRule / PlatformFeeRule / CustomerDiscountRule): rank candidates by specificity then priority, and <b>fail loud</b>
/// (<see cref="PaymentErrorCode.PartCommercialTermConflict"/>) on an ambiguous <c>(rank, priority)</c> tie — never a silent guess.
/// No persistence side effects. The confidential cost fields never leave this module: the application layer projects the resolved
/// entity into the cost-free <c>PartLineAllowanceDto</c>.
/// </summary>
public static class PartCommercialTermResolver
{
    /// <summary>Resolve the single effective term for <paramref name="ctx"/> from an already effective-date-filtered active set.</summary>
    public static PartCommercialTermEntity? Resolve(
        IEnumerable<PartCommercialTermEntity> scopedActiveTerms, PartCommercialTermResolveContext ctx)
    {
        var candidates = scopedActiveTerms
            .Where(t => IsCandidate(t, ctx))
            .Select(t => (term: t, rank: ComputeSpecificityRank(t)))
            .ToList();

        if (candidates.Count == 0) return null;

        var top = candidates
            .OrderByDescending(x => x.rank)
            .ThenByDescending(x => (int)x.term.Priority)
            .First();

        var tied = candidates.Where(x => x.rank == top.rank && x.term.Priority == top.term.Priority).ToList();
        if (tied.Count > 1)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.PartCommercialTermConflict,
                $"Part commercial term conflict: {tied.Count} active terms tie at specificity {top.rank} / " +
                $"priority {top.term.Priority}. Term ids: [{string.Join(", ", tied.Select(t => t.term.Id))}]. " +
                "Resolution refuses to guess — deactivate or re-prioritise one of them.");

        return top.term;
    }

    /// <summary>Create/update-time overlap guard: same scope + same Priority + overlapping half-open window (excludes self by Id).</summary>
    public static PartCommercialTermEntity? FindOverlappingConflict(
        PartCommercialTermEntity candidate, IEnumerable<PartCommercialTermEntity> existingActive)
        => existingActive.FirstOrDefault(e =>
               e.Id != candidate.Id
               && e.IsActive
               && EqualsCI(e.Brand, candidate.Brand)
               && EqualsCI(e.ProductCode, candidate.ProductCode)
               && e.ProviderProfileId == candidate.ProviderProfileId
               && EqualsCI(e.CategoryCode, candidate.CategoryCode)
               && EqualsCI(e.CurrencyCode, candidate.CurrencyCode)
               && e.Priority == candidate.Priority
               && Overlaps(candidate, e));

    /// <summary>Half-open <c>[from,to)</c> window overlap (<c>null</c> To = open-ended).</summary>
    public static bool Overlaps(PartCommercialTermEntity a, PartCommercialTermEntity b)
        => a.EffectiveFrom < (b.EffectiveTo ?? DateTime.MaxValue)
        && b.EffectiveFrom < (a.EffectiveTo ?? DateTime.MaxValue);

    /// <summary>Specificity rank: product(8) &gt; provider(4) &gt; brand(2) &gt; category(1) &gt; global(0). Powers of two so a higher
    /// dimension always outranks any combination of lower ones, while extra lower dims still break ties within the same top dim.</summary>
    public static int ComputeSpecificityRank(PartCommercialTermEntity t)
    {
        var rank = 0;
        if (!string.IsNullOrWhiteSpace(t.ProductCode)) rank += 8;
        if (t.ProviderProfileId.HasValue)              rank += 4;
        if (!string.IsNullOrWhiteSpace(t.Brand))       rank += 2;
        if (!string.IsNullOrWhiteSpace(t.CategoryCode)) rank += 1;
        return rank;
    }

    /// <summary>A term is a candidate only if its currency matches and every non-null scope dim equals the context's.</summary>
    public static bool IsCandidate(PartCommercialTermEntity t, PartCommercialTermResolveContext ctx)
    {
        if (!EqualsCI(t.CurrencyCode, ctx.CurrencyCode)) return false;
        if (!string.IsNullOrWhiteSpace(t.ProductCode) && !EqualsCI(t.ProductCode, ctx.ProductCode)) return false;
        if (t.ProviderProfileId.HasValue && t.ProviderProfileId != ctx.ProviderProfileId) return false;
        if (!string.IsNullOrWhiteSpace(t.Brand) && !EqualsCI(t.Brand, ctx.Brand)) return false;
        if (!string.IsNullOrWhiteSpace(t.CategoryCode) && !EqualsCI(t.CategoryCode, ctx.CategoryCode)) return false;
        return true;
    }

    private static bool EqualsCI(string? x, string? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;
        return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
    }
}
