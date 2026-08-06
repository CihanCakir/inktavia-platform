namespace Aizen.Bff.AdminPanel.Application.Payment.Dto;

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

// ─── List / Detail (BE-P6 admin surface) ──────────────────────────────────────
// Enum fields (DiscountType, FundingMode, Priority, Status) are `string` per the BFF Newtonsoft contract
// (the Payment module serializes enums as names).

/// <summary>A customer-discount rule as it appears in the admin list / detail.</summary>
public sealed record CustomerDiscountRuleListItemBffDto(
    long      Id,
    string?   RuleCode,
    long?     CustomerPlanId,
    string?   CategoryCode,
    string    CurrencyCode,
    string    DiscountType,          // "Percent" | "Fixed"
    decimal?  DiscountRate,
    decimal?  FixedDiscountAmount,
    decimal?  MinimumPurchaseAmount,
    decimal?  MaximumDiscountAmount,
    string    FundingMode,           // "PlatformFunded" | "ProviderFunded" | "Shared" | "SupplierFunded"
    decimal?  PlatformFundingRate,
    decimal?  ProviderFundingRate,
    bool      RequiresProviderConsent,
    string    Priority,              // CommissionRulePriority name
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string    Status,                // "Active" | "Scheduled" | "Expired" | "Inactive"
    bool      IsActive,
    string?   RuleName,
    string?   Notes,
    long?     CreateUserId,
    DateTime? CreateDate,
    long?     ModifyUserId,
    DateTime? ModifyDate
);

/// <summary>Customer-discount rule list response (no paging — rules are few).</summary>
public sealed record CustomerDiscountRuleListBffResult(
    List<CustomerDiscountRuleListItemBffDto> Items,
    int                                      Total
);

/// <summary>Full detail of a single customer-discount rule (same shape as the list item).</summary>
public sealed record CustomerDiscountRuleDetailBffDto(
    long      Id,
    string?   RuleCode,
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
    bool      RequiresProviderConsent,
    string    Priority,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string    Status,
    bool      IsActive,
    string?   RuleName,
    string?   Notes,
    long?     CreateUserId,
    DateTime? CreateDate,
    long?     ModifyUserId,
    DateTime? ModifyDate
);

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

// ─── List / Detail (BE-P6 admin surface — create + list + view MVP) ───────────
// RefundRestorePolicy + Status are `string` per the BFF Newtonsoft contract.

/// <summary>A per-plan benefit budget policy as it appears in the admin list / detail.</summary>
public sealed record CustomerBenefitBudgetPolicyListItemBffDto(
    long      Id,
    string?   PolicyCode,
    long      CustomerPlanId,
    string    CurrencyCode,
    decimal   BenefitBudgetRate,
    decimal?  PerPeriodMax,
    decimal?  PerCategoryLimit,
    decimal?  PerTransactionLimit,
    string    RefundRestorePolicy,   // "Restore" | "Consume"
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string    Status,                // "Active" | "Scheduled" | "Expired" | "Inactive"
    bool      IsActive,
    string?   Notes,
    long?     CreateUserId,
    DateTime? CreateDate,
    long?     ModifyUserId,
    DateTime? ModifyDate
);

/// <summary>Per-plan benefit budget policy list response (no paging — policies are few).</summary>
public sealed record CustomerBenefitBudgetPolicyListBffResult(
    List<CustomerBenefitBudgetPolicyListItemBffDto> Items,
    int                                             Total
);

/// <summary>Full detail of a single benefit budget policy (same shape as the list item).</summary>
public sealed record CustomerBenefitBudgetPolicyDetailBffDto(
    long      Id,
    string?   PolicyCode,
    long      CustomerPlanId,
    string    CurrencyCode,
    decimal   BenefitBudgetRate,
    decimal?  PerPeriodMax,
    decimal?  PerCategoryLimit,
    decimal?  PerTransactionLimit,
    string    RefundRestorePolicy,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string    Status,
    bool      IsActive,
    string?   Notes,
    long?     CreateUserId,
    DateTime? CreateDate,
    long?     ModifyUserId,
    DateTime? ModifyDate
);
