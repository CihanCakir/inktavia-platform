namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;

// ─── BE-P5 ProfitProtectionPolicy — BFF DTOs (AdjustmentOrder as string per the BFF enum contract) ──

/// <summary>Create a profit-protection policy (single-active per currency). AdjustmentOrder =
/// PlatformDiscountThenCommissionBenefit | CommissionBenefitThenPlatformDiscount.</summary>
public sealed record CreateProfitProtectionPolicyBffRequest(
    string    CurrencyCode,
    decimal   MinCustomerSideContributionAmount,
    decimal   MinCustomerSideContributionRate,
    decimal   MinProviderSideContributionAmount,
    decimal   MinProviderSideContributionRate,
    decimal   MinTransactionContributionAmount,
    decimal   MinTransactionContributionRate,
    decimal   PaymentProcessingExpenseRate,
    decimal   PaymentProcessingFixed,
    decimal   RefundRiskReserveRate,
    decimal   OtherVariableExpenseRate,
    decimal   OtherVariableExpenseFixed,
    decimal   CustomerSideVariableCostShareRate,
    string    AdjustmentOrder,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   PolicyName,
    string?   Notes);

/// <summary>Update a profit-protection policy. <c>Id</c> forced from the route; currency is fixed on the row.</summary>
public sealed record UpdateProfitProtectionPolicyBffRequest(
    long      Id,
    decimal   MinCustomerSideContributionAmount,
    decimal   MinCustomerSideContributionRate,
    decimal   MinProviderSideContributionAmount,
    decimal   MinProviderSideContributionRate,
    decimal   MinTransactionContributionAmount,
    decimal   MinTransactionContributionRate,
    decimal   PaymentProcessingExpenseRate,
    decimal   PaymentProcessingFixed,
    decimal   RefundRiskReserveRate,
    decimal   OtherVariableExpenseRate,
    decimal   OtherVariableExpenseFixed,
    decimal   CustomerSideVariableCostShareRate,
    string    AdjustmentOrder,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   PolicyName,
    string?   Notes);

public sealed record ProfitProtectionPolicyCreateBffResult(long Id, string PolicyCode);
public sealed record ProfitProtectionPolicyMutateBffResult(long Id, string? PolicyCode);

/// <summary>Resolved active policy (preview). AdjustmentOrder as string per the BFF enum contract.</summary>
public sealed record ProfitProtectionPolicyResolveBffResult(
    long      PolicyId,
    string?   PolicyCode,
    string    CurrencyCode,
    decimal   MinCustomerSideContributionAmount,
    decimal   MinCustomerSideContributionRate,
    decimal   MinProviderSideContributionAmount,
    decimal   MinProviderSideContributionRate,
    decimal   MinTransactionContributionAmount,
    decimal   MinTransactionContributionRate,
    decimal   PaymentProcessingExpenseRate,
    decimal   PaymentProcessingFixed,
    decimal   RefundRiskReserveRate,
    decimal   OtherVariableExpenseRate,
    decimal   OtherVariableExpenseFixed,
    decimal   CustomerSideVariableCostShareRate,
    string    AdjustmentOrder,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo);
