namespace Aizen.Bff.AdminPanel.Application.Payment.Dto;

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

// ─── List / Detail (BE-P5 admin surface — version history + editor) ───────────
// AdjustmentOrder + Status are `string` per the BFF Newtonsoft contract (the module serializes enums as names).

/// <summary>A profit-protection policy as it appears in the admin version-history list / detail.</summary>
public sealed record ProfitProtectionPolicyListItemBffDto(
    long      Id,
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
    string    AdjustmentOrder,   // "PlatformDiscountThenCommissionBenefit" | "CommissionBenefitThenPlatformDiscount"
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string    Status,            // "Active" | "Scheduled" | "Expired" | "Inactive"
    bool      IsActive,
    string?   PolicyName,
    string?   Notes,
    long?     CreateUserId,
    DateTime? CreateDate,
    long?     ModifyUserId,
    DateTime? ModifyDate
);

/// <summary>Version-history list response (no paging — policies are few).</summary>
public sealed record ProfitProtectionPolicyListBffResult(
    List<ProfitProtectionPolicyListItemBffDto> Items,
    int                                        Total
);

/// <summary>Full detail of a single profit-protection policy (same shape as the list item).</summary>
public sealed record ProfitProtectionPolicyDetailBffDto(
    long      Id,
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
    DateTime? EffectiveTo,
    string    Status,
    bool      IsActive,
    string?   PolicyName,
    string?   Notes,
    long?     CreateUserId,
    DateTime? CreateDate,
    long?     ModifyUserId,
    DateTime? ModifyDate
);

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
