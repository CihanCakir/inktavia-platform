using Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;

namespace Aizen.Modules.Payment.Application.Dto;

/// <summary>
/// Maps a <see cref="CustomerBenefitBudgetPolicyEntity"/> to the admin <see cref="CustomerBenefitBudgetPolicyDto"/>.
/// Kept in one place so the list and detail query handlers stay in lock-step.
/// </summary>
public static class CustomerBenefitBudgetPolicyDtoMapper
{
    public static CustomerBenefitBudgetPolicyDto ToDto(CustomerBenefitBudgetPolicyEntity p) => new(
        p.Id,
        p.PolicyCode,
        p.CustomerPlanId,
        p.CurrencyCode,
        p.BenefitBudgetRate,
        p.PerPeriodMax,
        p.PerCategoryLimit,
        p.PerTransactionLimit,
        p.RefundRestorePolicy,
        p.EffectiveFrom,
        p.EffectiveTo,
        p.Status,
        p.IsActive,
        p.Notes,
        p.CreateUserId,
        p.CreateDate,
        p.ModifyUserId,
        p.ModifyDate);
}
