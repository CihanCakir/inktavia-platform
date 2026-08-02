using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;

namespace Aizen.Modules.Payment.Application.Dto;

/// <summary>
/// Maps a <see cref="ProviderCommissionBenefitRuleEntity"/> to the admin <see cref="ProviderCommissionBenefitRuleDto"/>.
/// Kept in one place so the list and detail query handlers stay in lock-step.
/// </summary>
public static class ProviderCommissionBenefitRuleDtoMapper
{
    public static ProviderCommissionBenefitRuleDto ToDto(ProviderCommissionBenefitRuleEntity r) => new(
        r.Id,
        r.RuleCode,
        r.RuleName,
        r.ProviderProfileId,
        r.ProviderPlanId,
        r.ApplicableCategoryCodes.ToList(),
        r.CurrencyCode,
        r.AdjustmentPercentagePoints,
        r.MinimumCommissionRate,
        r.MaximumDiscountAmount,
        r.MaximumEligibleGMV,
        r.UsageLimit,
        r.Stackable,
        r.Exclusive,
        r.Priority,
        r.EffectiveFrom,
        r.EffectiveTo,
        r.Status,
        r.IsActive,
        r.Notes,
        r.CreateUserId,
        r.CreateDate,
        r.ModifyUserId,
        r.ModifyDate);
}
