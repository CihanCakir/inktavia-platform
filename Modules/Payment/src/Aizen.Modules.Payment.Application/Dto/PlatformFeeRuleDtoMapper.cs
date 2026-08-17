using Aizen.Modules.Payment.Domain.Entities.PlatformFee;

namespace Aizen.Modules.Payment.Application.Dto;

/// <summary>
/// Maps a <see cref="PlatformFeeRuleEntity"/> to the admin <see cref="PlatformFeeRuleDto"/>. Kept in one place so the
/// list and detail query handlers stay in lock-step. SpecificityRank is delegated to the domain resolver.
/// </summary>
public static class PlatformFeeRuleDtoMapper
{
    public static PlatformFeeRuleDto ToDto(PlatformFeeRuleEntity r) => new(
        r.Id,
        r.RuleCode,
        r.Model,
        r.Rate,
        r.FixedAmount,
        r.MinAmount,
        r.MaxAmount,
        r.CurrencyCode,
        r.CategoryCode,
        r.CustomerType,
        r.Priority,
        r.EffectiveFrom,
        r.EffectiveTo,
        r.Status,
        r.IsActive,
        r.RuleName,
        r.Notes,
        r.VatRate,
        PlatformFeeRuleResolver.ComputeSpecificityRank(r),
        r.CreateUserId,
        r.CreateDate,
        r.ModifyUserId,
        r.ModifyDate);
}
