using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;

/// <summary>
/// Pure single-active resolution + overlap guard for <see cref="CustomerBenefitBudgetPolicyEntity"/> (per plan + currency,
/// mirrors BE-P4/P5). Overlap → fail-loud <see cref="PaymentErrorCode.CustomerBenefitBudgetPolicyConflict"/>.
/// </summary>
public static class CustomerBenefitBudgetPolicyResolver
{
    public static CustomerBenefitBudgetPolicyEntity? Resolve(
        IEnumerable<CustomerBenefitBudgetPolicyEntity> active, long customerPlanId, string currency, DateTime atUtc)
    {
        var matches = active
            .Where(p => p.CustomerPlanId == customerPlanId
                     && string.Equals(p.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)
                     && p.IsEffective(atUtc))
            .ToList();

        if (matches.Count > 1)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.CustomerBenefitBudgetPolicyConflict,
                $"Overlapping benefit budget policies for plan {customerPlanId}/{currency} at {atUtc:o} " +
                $"(ids: [{string.Join(", ", matches.Select(m => m.Id))}]).");

        return matches.Count == 1 ? matches[0] : null;
    }

    public static CustomerBenefitBudgetPolicyEntity? FindOverlappingConflict(
        CustomerBenefitBudgetPolicyEntity candidate, IEnumerable<CustomerBenefitBudgetPolicyEntity> existingActive)
        => existingActive.FirstOrDefault(e =>
               e.Id != candidate.Id
               && e.IsActive
               && e.CustomerPlanId == candidate.CustomerPlanId
               && string.Equals(e.CurrencyCode, candidate.CurrencyCode, StringComparison.OrdinalIgnoreCase)
               && candidate.EffectiveFrom < (e.EffectiveTo ?? DateTime.MaxValue)
               && e.EffectiveFrom < (candidate.EffectiveTo ?? DateTime.MaxValue));
}
