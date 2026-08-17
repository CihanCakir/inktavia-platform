using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Dto;

/// <summary>
/// Application-layer DTO for customer-discount rule admin management (BE-P6). Mirrors the P3 platform-fee / P5
/// profit-protection admin DTO shape (used for both list and detail). All enum fields are C# enum types — the Payment
/// module API's StringEnumConverter serializes them as strings to upstream callers. Unlike profit-protection this is a
/// specificity + Priority rule (plan / category / global scope), so it carries <see cref="Priority"/> and the targeting
/// dimensions the admin editor derives specificity from.
/// </summary>
public sealed record CustomerDiscountRuleDto(
    long                        Id,
    string?                     RuleCode,
    // ── Targeting ────────────────────────────────────────────────────────────────
    long?                       CustomerPlanId,
    string?                     CategoryCode,
    string                      CurrencyCode,
    // ── Discount definition ──────────────────────────────────────────────────────
    CustomerDiscountType        DiscountType,
    decimal?                    DiscountRate,
    decimal?                    FixedDiscountAmount,
    decimal?                    MinimumPurchaseAmount,
    decimal?                    MaximumDiscountAmount,
    // ── Funding (§19.6) ──────────────────────────────────────────────────────────
    CustomerDiscountFundingMode FundingMode,
    decimal?                    PlatformFundingRate,
    decimal?                    ProviderFundingRate,
    bool                        RequiresProviderConsent,
    // ── Lifecycle / admin ───────────────────────────────────────────────────────
    CommissionRulePriority      Priority,
    DateTime                    EffectiveFrom,
    DateTime?                   EffectiveTo,
    CommissionRuleStatus        Status,
    bool                        IsActive,
    string?                     RuleName,
    string?                     Notes,
    long?                       CreateUserId,
    DateTime?                   CreateDate,
    long?                       ModifyUserId,
    DateTime?                   ModifyDate
);

/// <summary>Customer-discount rule list (no paging — mirrors the P3/P5 admin surface).</summary>
public sealed record CustomerDiscountRuleListResult(
    List<CustomerDiscountRuleDto> Items,
    int                           Total
);
