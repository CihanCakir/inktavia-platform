using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.ProfitProtection;

/// <summary>
/// Pure, side-effect-free profit-protection policy resolution + single-active guard (§19.3, mirrors BE-P4).
/// Exactly one Active policy per currency at any instant; overlap → fail-loud conflict. No DB, fully unit-testable.
/// </summary>
public static class ProfitProtectionPolicyResolver
{
    /// <summary>
    /// Point-in-time resolution: the single Active policy for <paramref name="currency"/> covering
    /// <paramref name="atUtc"/> on [EffectiveFrom, EffectiveTo). &gt;1 → throws
    /// <see cref="PaymentErrorCode.ProfitProtectionPolicyConflict"/>. 0 → returns null (caller maps to
    /// ProfitProtectionPolicyNotFound → ConfigurationError at the engine).
    /// </summary>
    public static ProfitProtectionPolicyEntity? Resolve(
        IEnumerable<ProfitProtectionPolicyEntity> activePolicies, string currency, DateTime atUtc)
    {
        var matches = activePolicies
            .Where(p => string.Equals(p.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)
                     && p.IsEffective(atUtc))
            .ToList();

        if (matches.Count > 1)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.ProfitProtectionPolicyConflict,
                $"Overlapping profit-protection policies for {currency} at {atUtc:o}: " +
                $"{matches.Count} active (ids: [{string.Join(", ", matches.Select(m => m.Id))}]). Exactly one must apply.");

        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Create/Update single-active guard: returns the first existing active policy of the same currency whose window
    /// overlaps <paramref name="candidate"/>, or null. Candidate excluded by Id.
    /// </summary>
    public static ProfitProtectionPolicyEntity? FindOverlappingConflict(
        ProfitProtectionPolicyEntity candidate,
        IEnumerable<ProfitProtectionPolicyEntity> existingActive)
        => existingActive.FirstOrDefault(e =>
               e.Id != candidate.Id
               && e.IsActive
               && string.Equals(e.CurrencyCode, candidate.CurrencyCode, StringComparison.OrdinalIgnoreCase)
               && candidate.EffectiveFrom < (e.EffectiveTo ?? DateTime.MaxValue)
               && e.EffectiveFrom < (candidate.EffectiveTo ?? DateTime.MaxValue));
}
