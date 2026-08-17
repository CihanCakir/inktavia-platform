using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Dto;

/// <summary>
/// Application-layer DTO for provider commission-benefit rule admin management (BE-P7). Mirrors the P6 customer-discount
/// admin DTO shape (used for both list and detail). All enum fields are C# enum types — the Payment module API's
/// StringEnumConverter serializes them as strings to upstream callers. A specificity + Priority rule (provider / plan /
/// category scope), so it carries <see cref="Priority"/> and the targeting dimensions the admin editor derives
/// specificity from. Benefits only LOWER commission: <see cref="AdjustmentPercentagePoints"/> is ≤ 0.
/// </summary>
public sealed record ProviderCommissionBenefitRuleDto(
    long                   Id,
    string?                RuleCode,
    string?                RuleName,
    // ── Targeting ────────────────────────────────────────────────────────────────
    long?                  ProviderProfileId,
    long?                  ProviderPlanId,
    List<string>           ApplicableCategoryCodes,
    string                 CurrencyCode,
    // ── Benefit definition ──────────────────────────────────────────────────────
    decimal                AdjustmentPercentagePoints,
    decimal                MinimumCommissionRate,
    decimal?               MaximumDiscountAmount,
    decimal?               MaximumEligibleGMV,
    long?                  UsageLimit,
    bool                   Stackable,
    bool                   Exclusive,
    // ── Lifecycle / admin ───────────────────────────────────────────────────────
    CommissionRulePriority Priority,
    DateTime               EffectiveFrom,
    DateTime?              EffectiveTo,
    CommissionRuleStatus   Status,
    bool                   IsActive,
    string?                Notes,
    long?                  CreateUserId,
    DateTime?              CreateDate,
    long?                  ModifyUserId,
    DateTime?              ModifyDate
);

/// <summary>Provider commission-benefit rule list (no paging — mirrors the P3/P5/P6 admin surface).</summary>
public sealed record ProviderCommissionBenefitRuleListResult(
    List<ProviderCommissionBenefitRuleDto> Items,
    int                                    Total
);
