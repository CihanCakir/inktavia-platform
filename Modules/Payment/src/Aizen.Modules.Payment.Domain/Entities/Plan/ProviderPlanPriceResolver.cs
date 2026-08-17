using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Plan;

/// <summary>Outcome of the create/update overlap+gap guard (§4/§13.1).</summary>
public enum PlanPriceGuardOutcome { Ok, Overlap, Gap }

/// <summary>Guard result; <see cref="ConflictingId"/> is set for an Overlap.</summary>
public sealed record PlanPriceGuardResult(PlanPriceGuardOutcome Outcome, long? ConflictingId = null);

/// <summary>
/// Pure, side-effect-free provider plan price resolution + contiguity guard (BE-P4, §4/§13.1). Kept in the domain
/// (no DB) so point-in-time resolution and the overlap/gap invariant are fully unit-testable. The repository
/// feeds it the scoped rule set; it never writes.
/// </summary>
public static class ProviderPlanPriceResolver
{
    /// <summary>
    /// Point-in-time resolution: from <paramref name="scopedActivePrices"/> (already filtered to one
    /// (plan, currency, billing period) and Active) returns the single price covering <paramref name="atUtc"/>
    /// on the half-open range [EffectiveFrom, EffectiveTo). &gt;1 match → throws
    /// <see cref="AizenBusinessException"/>(<see cref="PaymentErrorCode.ProviderPlanPriceConflict"/>) (overlap =
    /// configuration error). 0 match → returns null (caller decides gap / not-found). Never a silent FirstOrDefault.
    /// </summary>
    public static ProviderPlanPriceEntity? Resolve(
        IEnumerable<ProviderPlanPriceEntity> scopedActivePrices, DateTime atUtc)
    {
        var matches = scopedActivePrices.Where(p => p.CoversInstant(atUtc)).ToList();

        if (matches.Count > 1)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.ProviderPlanPriceConflict,
                $"Overlapping plan prices at {atUtc:o}: {matches.Count} active records cover the same instant " +
                $"(ids: [{string.Join(", ", matches.Select(m => m.Id))}]). Exactly one must apply.");

        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Create/Update guard (§4): validates that adding/replacing <paramref name="candidate"/> keeps the
    /// (plan, currency, billing period) chain non-overlapping AND gap-free (consecutive rows contiguous:
    /// prev.EffectiveTo == next.EffectiveFrom). <paramref name="existingActiveSameScope"/> is all Active/Scheduled
    /// rows in the same scope; the candidate is excluded by Id (so an update isn't flagged against itself).
    /// </summary>
    public static PlanPriceGuardResult ValidateInsertable(
        ProviderPlanPriceEntity candidate,
        IEnumerable<ProviderPlanPriceEntity> existingActiveSameScope)
    {
        var others = existingActiveSameScope.Where(e => e.Id != candidate.Id).ToList();

        // 1. Overlap — no two ranges may cover a shared instant.
        var overlap = others.FirstOrDefault(e => Overlaps(candidate, e));
        if (overlap is not null)
            return new PlanPriceGuardResult(PlanPriceGuardOutcome.Overlap, overlap.Id);

        // 2. Gap — consecutive rows must be contiguous (prev.EffectiveTo == next.EffectiveFrom).
        var chain = others.Append(candidate).OrderBy(x => x.EffectiveFrom).ToList();
        for (var i = 1; i < chain.Count; i++)
            if (chain[i - 1].EffectiveTo != chain[i].EffectiveFrom)
                return new PlanPriceGuardResult(PlanPriceGuardOutcome.Gap);

        return new PlanPriceGuardResult(PlanPriceGuardOutcome.Ok);
    }

    /// <summary>Half-open interval overlap of [from, to) with null <c>to</c> meaning +infinity.</summary>
    public static bool Overlaps(ProviderPlanPriceEntity a, ProviderPlanPriceEntity b)
        => a.EffectiveFrom < (b.EffectiveTo ?? DateTime.MaxValue)
        && b.EffectiveFrom < (a.EffectiveTo ?? DateTime.MaxValue);
}
