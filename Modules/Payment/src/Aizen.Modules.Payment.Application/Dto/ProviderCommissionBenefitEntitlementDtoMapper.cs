using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;

namespace Aizen.Modules.Payment.Application.Dto;

/// <summary>
/// Maps a <see cref="ProviderCommissionBenefitEntitlementEntity"/> to the admin
/// <see cref="ProviderCommissionBenefitEntitlementDto"/>. The optional <paramref name="rule"/> supplies the referenced
/// benefit rule's code/name so the list/detail read without a second round-trip.
/// </summary>
public static class ProviderCommissionBenefitEntitlementDtoMapper
{
    public static ProviderCommissionBenefitEntitlementDto ToDto(
        ProviderCommissionBenefitEntitlementEntity e, ProviderCommissionBenefitRuleEntity? rule = null) => new(
        e.Id,
        e.EntitlementCode,
        e.ProviderProfileId,
        e.BenefitRuleId,
        rule?.RuleCode,
        rule?.RuleName,
        e.GrantedFrom,
        e.GrantedTo,
        e.UsageLimit,
        e.UsedCount,
        e.ReservedCount,
        e.RemainingUsage,
        e.MaximumEligibleGMV,
        e.ConsumedGMV,
        e.ReservedGMV,
        e.RemainingGmv,
        e.Status,
        e.IsActive,
        e.CreateUserId,
        e.CreateDate,
        e.ModifyUserId,
        e.ModifyDate);
}
