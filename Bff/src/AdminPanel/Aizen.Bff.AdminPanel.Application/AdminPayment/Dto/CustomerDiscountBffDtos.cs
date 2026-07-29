namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;

// ─── BE-P6 CustomerDiscountRule + CustomerBenefitBudgetPolicy — BFF DTOs ──────
// Enum fields as string: DiscountType (Percent|Fixed), FundingMode (PlatformFunded|ProviderFunded|Shared|SupplierFunded),
// Priority, RefundRestorePolicy (Restore|Consume).

/// <summary>Create a customer-discount rule.</summary>
public sealed record CreateCustomerDiscountRuleBffRequest(
    long?     CustomerPlanId,
    string?   CategoryCode,
    string    CurrencyCode,
    string    DiscountType,
    decimal?  DiscountRate,
    decimal?  FixedDiscountAmount,
    decimal?  MinimumPurchaseAmount,
    decimal?  MaximumDiscountAmount,
    string    FundingMode,
    decimal?  PlatformFundingRate,
    decimal?  ProviderFundingRate,
    bool?     RequiresProviderConsent,
    string    Priority,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   RuleName,
    string?   Notes);

/// <summary>Update a customer-discount rule. <c>Id</c> forced from the route; plan/category/currency are fixed.</summary>
public sealed record UpdateCustomerDiscountRuleBffRequest(
    long      Id,
    string    DiscountType,
    decimal?  DiscountRate,
    decimal?  FixedDiscountAmount,
    decimal?  MinimumPurchaseAmount,
    decimal?  MaximumDiscountAmount,
    string    FundingMode,
    decimal?  PlatformFundingRate,
    decimal?  ProviderFundingRate,
    bool      RequiresProviderConsent,
    string    Priority,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   RuleName,
    string?   Notes);

public sealed record CustomerDiscountRuleCreateBffResult(long Id, string RuleCode);
public sealed record CustomerDiscountRuleMutateBffResult(long Id, string? RuleCode);

/// <summary>Resolved customer-discount + funding split (preview).</summary>
public sealed record CustomerDiscountResolveBffResult(
    long?    DiscountRuleId,
    decimal  RequestedDiscountAmount,
    decimal  RequestedPlatformFundedCustomerDiscount,
    decimal  ProviderFundedCustomerDiscount,
    decimal  AppliedDiscountAmount,
    decimal  UnappliedDueToConsent,
    decimal  CustomerBenefitBudgetRemaining,
    decimal  CustomerPlanRevenueAllocation);

// ─── CustomerBenefitBudgetPolicy (per plan) ──────────────────────────────────

/// <summary>Create a per-plan customer-benefit budget policy. RefundRestorePolicy = Restore | Consume.</summary>
public sealed record CreateCustomerBenefitBudgetPolicyBffRequest(
    long      CustomerPlanId,
    string    CurrencyCode,
    decimal   BenefitBudgetRate,
    decimal?  PerPeriodMax,
    decimal?  PerCategoryLimit,
    decimal?  PerTransactionLimit,
    string    RefundRestorePolicy,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   Notes);

public sealed record CustomerBenefitBudgetPolicyCreateBffResult(long Id, string PolicyCode);
