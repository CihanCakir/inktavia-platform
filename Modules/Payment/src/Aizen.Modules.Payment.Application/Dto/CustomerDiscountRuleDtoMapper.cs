using Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;

namespace Aizen.Modules.Payment.Application.Dto;

/// <summary>
/// Maps a <see cref="CustomerDiscountRuleEntity"/> to the admin <see cref="CustomerDiscountRuleDto"/>. Kept in one place
/// so the list and detail query handlers stay in lock-step.
/// </summary>
public static class CustomerDiscountRuleDtoMapper
{
    public static CustomerDiscountRuleDto ToDto(CustomerDiscountRuleEntity r) => new(
        r.Id,
        r.RuleCode,
        r.CustomerPlanId,
        r.CategoryCode,
        r.CurrencyCode,
        r.DiscountType,
        r.DiscountRate,
        r.FixedDiscountAmount,
        r.MinimumPurchaseAmount,
        r.MaximumDiscountAmount,
        r.FundingMode,
        r.PlatformFundingRate,
        r.ProviderFundingRate,
        r.RequiresProviderConsent,
        r.Priority,
        r.EffectiveFrom,
        r.EffectiveTo,
        r.Status,
        r.IsActive,
        r.RuleName,
        r.Notes,
        r.CreateUserId,
        r.CreateDate,
        r.ModifyUserId,
        r.ModifyDate);
}
