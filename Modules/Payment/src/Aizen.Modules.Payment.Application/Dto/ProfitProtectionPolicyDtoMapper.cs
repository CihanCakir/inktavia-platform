using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;

namespace Aizen.Modules.Payment.Application.Dto;

/// <summary>
/// Maps a <see cref="ProfitProtectionPolicyEntity"/> to the admin <see cref="ProfitProtectionPolicyDto"/>. Kept in one
/// place so the list and detail query handlers stay in lock-step.
/// </summary>
public static class ProfitProtectionPolicyDtoMapper
{
    public static ProfitProtectionPolicyDto ToDto(ProfitProtectionPolicyEntity p) => new(
        p.Id,
        p.PolicyCode,
        p.CurrencyCode,
        p.MinCustomerSideContributionAmount,
        p.MinCustomerSideContributionRate,
        p.MinProviderSideContributionAmount,
        p.MinProviderSideContributionRate,
        p.MinTransactionContributionAmount,
        p.MinTransactionContributionRate,
        p.PaymentProcessingExpenseRate,
        p.PaymentProcessingFixed,
        p.RefundRiskReserveRate,
        p.OtherVariableExpenseRate,
        p.OtherVariableExpenseFixed,
        p.CustomerSideVariableCostShareRate,
        p.AdjustmentOrder,
        p.EffectiveFrom,
        p.EffectiveTo,
        p.Status,
        p.IsActive,
        p.PolicyName,
        p.Notes,
        p.CreateUserId,
        p.CreateDate,
        p.ModifyUserId,
        p.ModifyDate);
}
