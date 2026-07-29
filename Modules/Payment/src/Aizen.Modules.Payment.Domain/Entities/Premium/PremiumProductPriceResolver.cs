using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Premium;

/// <summary>Outcome of the create/update overlap+gap guard (BE-P11 §9.1, mirrors BE-P4).</summary>
public enum PremiumPriceGuardOutcome { Ok, Overlap, Gap }

/// <summary>Guard result; <see cref="ConflictingId"/> is set for an Overlap.</summary>
public sealed record PremiumPriceGuardResult(PremiumPriceGuardOutcome Outcome, long? ConflictingId = null);

/// <summary>
/// Pure, side-effect-free premium-price resolution + contiguity guard (BE-P11 §9.1/§13.9). Mirrors
/// <c>ProviderPlanPriceResolver</c>: the repository feeds it the scoped active rows; it never writes, so point-in-time
/// resolution and the overlap/gap invariant are fully unit-testable.
/// </summary>
public static class PremiumProductPriceResolver
{
    /// <summary>
    /// Point-in-time resolution: from <paramref name="scopedActivePrices"/> (already filtered to one (product, currency)
    /// and Active) returns the single price covering <paramref name="atUtc"/> on the half-open range. &gt;1 match → throws
    /// <see cref="PaymentErrorCode.PremiumProductPriceConflict"/> (overlap = config error). 0 match → returns null.
    /// </summary>
    public static PremiumProductPriceEntity? Resolve(
        IEnumerable<PremiumProductPriceEntity> scopedActivePrices, DateTime atUtc)
    {
        var matches = scopedActivePrices.Where(p => p.CoversInstant(atUtc)).ToList();

        if (matches.Count > 1)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.PremiumProductPriceConflict,
                $"Overlapping premium prices at {atUtc:o}: {matches.Count} active records cover the same instant " +
                $"(ids: [{string.Join(", ", matches.Select(m => m.Id))}]). Exactly one must apply.");

        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Create/Update guard: validates that adding/replacing <paramref name="candidate"/> keeps the (product, currency)
    /// chain non-overlapping AND gap-free (consecutive rows contiguous). <paramref name="existingActiveSameScope"/> is all
    /// Active/Scheduled rows in the same scope; the candidate is excluded by Id so an update isn't flagged against itself.
    /// </summary>
    public static PremiumPriceGuardResult ValidateInsertable(
        PremiumProductPriceEntity candidate,
        IEnumerable<PremiumProductPriceEntity> existingActiveSameScope)
    {
        var others = existingActiveSameScope.Where(e => e.Id != candidate.Id).ToList();

        var overlap = others.FirstOrDefault(e => Overlaps(candidate, e));
        if (overlap is not null)
            return new PremiumPriceGuardResult(PremiumPriceGuardOutcome.Overlap, overlap.Id);

        var chain = others.Append(candidate).OrderBy(x => x.EffectiveFrom).ToList();
        for (var i = 1; i < chain.Count; i++)
            if (chain[i - 1].EffectiveTo != chain[i].EffectiveFrom)
                return new PremiumPriceGuardResult(PremiumPriceGuardOutcome.Gap);

        return new PremiumPriceGuardResult(PremiumPriceGuardOutcome.Ok);
    }

    /// <summary>Half-open interval overlap of [from, to) with null <c>to</c> meaning +infinity.</summary>
    public static bool Overlaps(PremiumProductPriceEntity a, PremiumProductPriceEntity b)
        => a.EffectiveFrom < (b.EffectiveTo ?? DateTime.MaxValue)
        && b.EffectiveFrom < (a.EffectiveTo ?? DateTime.MaxValue);
}
