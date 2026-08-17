using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Dto;

/// <summary>
/// Application-layer DTO for customer-benefit budget policy admin management (BE-P6). A per-plan, versioned policy — no
/// specificity / priority (unlike the discount rule). All enum fields are C# enum types — the Payment module API's
/// StringEnumConverter serializes them as strings to upstream callers. MVP surface: create + list + view (no update /
/// deactivate / reactivate in the module today).
/// </summary>
public sealed record CustomerBenefitBudgetPolicyDto(
    long                       Id,
    string?                    PolicyCode,
    long                       CustomerPlanId,
    string                     CurrencyCode,
    decimal                    BenefitBudgetRate,
    decimal?                   PerPeriodMax,
    decimal?                   PerCategoryLimit,
    decimal?                   PerTransactionLimit,
    BenefitRefundRestorePolicy RefundRestorePolicy,
    DateTime                   EffectiveFrom,
    DateTime?                  EffectiveTo,
    CommissionRuleStatus       Status,
    bool                       IsActive,
    string?                    Notes,
    long?                      CreateUserId,
    DateTime?                  CreateDate,
    long?                      ModifyUserId,
    DateTime?                  ModifyDate
);

/// <summary>Customer-benefit budget policy list (no paging — policies are few).</summary>
public sealed record CustomerBenefitBudgetPolicyListResult(
    List<CustomerBenefitBudgetPolicyDto> Items,
    int                                  Total
);
