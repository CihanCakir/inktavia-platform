namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;

// ─── BE-P7 ProviderCommissionBenefitRule + entitlement — BFF DTOs ────────────
// Stackable/Exclusive are bools (not an enum). Priority is a string per the BFF enum contract.

/// <summary>Create a provider-commission-benefit rule (must not be both Stackable AND Exclusive).</summary>
public sealed record CreateProviderCommissionBenefitRuleBffRequest(
    string?        RuleName,
    long?          ProviderProfileId,
    long?          ProviderPlanId,
    List<string>?  ApplicableCategoryCodes,
    decimal        AdjustmentPercentagePoints,
    decimal        MinimumCommissionRate,
    decimal?       MaximumDiscountAmount,
    decimal?       MaximumEligibleGMV,
    long?          UsageLimit,
    bool           Stackable,
    bool           Exclusive,
    string         Priority,
    DateTime       EffectiveFrom,
    DateTime?      EffectiveTo,
    string         CurrencyCode,
    string?        Notes);

/// <summary>Update a provider-commission-benefit rule. <c>Id</c> forced from the route; provider/plan/currency are fixed.</summary>
public sealed record UpdateProviderCommissionBenefitRuleBffRequest(
    long           Id,
    string?        RuleName,
    List<string>?  ApplicableCategoryCodes,
    decimal        AdjustmentPercentagePoints,
    decimal        MinimumCommissionRate,
    decimal?       MaximumDiscountAmount,
    decimal?       MaximumEligibleGMV,
    long?          UsageLimit,
    bool           Stackable,
    bool           Exclusive,
    string         Priority,
    DateTime       EffectiveFrom,
    DateTime?      EffectiveTo,
    string?        Notes);

public sealed record ProviderCommissionBenefitRuleCreateBffResult(long Id, string RuleCode);
public sealed record ProviderCommissionBenefitRuleMutateBffResult(long Id, string? RuleCode);

// ─── Rule list / detail (enum fields are strings — the module serializes enum names) ──

/// <summary>A provider-commission-benefit rule as it appears in the admin list / detail.</summary>
public sealed record ProviderCommissionBenefitRuleListItemBffDto(
    long          Id,
    string?       RuleCode,
    string?       RuleName,
    long?         ProviderProfileId,
    long?         ProviderPlanId,
    List<string>  ApplicableCategoryCodes,
    string        CurrencyCode,
    decimal       AdjustmentPercentagePoints,
    decimal       MinimumCommissionRate,
    decimal?      MaximumDiscountAmount,
    decimal?      MaximumEligibleGMV,
    long?         UsageLimit,
    bool          Stackable,
    bool          Exclusive,
    string        Priority,              // CommissionRulePriority name
    DateTime      EffectiveFrom,
    DateTime?     EffectiveTo,
    string        Status,                // "Active" | "Scheduled" | "Expired" | "Inactive"
    bool          IsActive,
    string?       Notes,
    long?         CreateUserId,
    DateTime?     CreateDate,
    long?         ModifyUserId,
    DateTime?     ModifyDate
);

/// <summary>Provider-commission-benefit rule list response (no paging — rules are few).</summary>
public sealed record ProviderCommissionBenefitRuleListBffResult(
    List<ProviderCommissionBenefitRuleListItemBffDto> Items,
    int                                               Total
);

/// <summary>Full detail of a single provider-commission-benefit rule (same shape as the list item).</summary>
public sealed record ProviderCommissionBenefitRuleDetailBffDto(
    long          Id,
    string?       RuleCode,
    string?       RuleName,
    long?         ProviderProfileId,
    long?         ProviderPlanId,
    List<string>  ApplicableCategoryCodes,
    string        CurrencyCode,
    decimal       AdjustmentPercentagePoints,
    decimal       MinimumCommissionRate,
    decimal?      MaximumDiscountAmount,
    decimal?      MaximumEligibleGMV,
    long?         UsageLimit,
    bool          Stackable,
    bool          Exclusive,
    string        Priority,
    DateTime      EffectiveFrom,
    DateTime?     EffectiveTo,
    string        Status,
    bool          IsActive,
    string?       Notes,
    long?         CreateUserId,
    DateTime?     CreateDate,
    long?         ModifyUserId,
    DateTime?     ModifyDate
);

// ─── Entitlement list / detail ────────────────────────────────────────────────

/// <summary>A provider-commission-benefit entitlement as it appears in the admin list / detail (usage counters read-only).</summary>
public sealed record ProviderCommissionBenefitEntitlementListItemBffDto(
    long      Id,
    string?   EntitlementCode,
    long      ProviderProfileId,
    long      BenefitRuleId,
    string?   BenefitRuleCode,
    string?   BenefitRuleName,
    DateTime  GrantedFrom,
    DateTime? GrantedTo,
    long?     UsageLimit,
    long      UsedCount,
    long      ReservedCount,
    long?     RemainingUsage,
    decimal?  MaximumEligibleGMV,
    decimal   ConsumedGMV,
    decimal   ReservedGMV,
    decimal?  RemainingGmv,
    string    Status,                // "Active" | "Exhausted" | "Expired" | "Revoked"
    bool      IsActive,
    long?     CreateUserId,
    DateTime? CreateDate,
    long?     ModifyUserId,
    DateTime? ModifyDate
);

/// <summary>Provider-commission-benefit entitlement list response (no paging).</summary>
public sealed record ProviderCommissionBenefitEntitlementListBffResult(
    List<ProviderCommissionBenefitEntitlementListItemBffDto> Items,
    int                                                      Total
);

/// <summary>Full detail of a single entitlement (same shape as the list item).</summary>
public sealed record ProviderCommissionBenefitEntitlementDetailBffDto(
    long      Id,
    string?   EntitlementCode,
    long      ProviderProfileId,
    long      BenefitRuleId,
    string?   BenefitRuleCode,
    string?   BenefitRuleName,
    DateTime  GrantedFrom,
    DateTime? GrantedTo,
    long?     UsageLimit,
    long      UsedCount,
    long      ReservedCount,
    long?     RemainingUsage,
    decimal?  MaximumEligibleGMV,
    decimal   ConsumedGMV,
    decimal   ReservedGMV,
    decimal?  RemainingGmv,
    string    Status,
    bool      IsActive,
    long?     CreateUserId,
    DateTime? CreateDate,
    long?     ModifyUserId,
    DateTime? ModifyDate
);

/// <summary>Grant a provider an entitlement to a benefit rule.</summary>
public sealed record GrantProviderCommissionBenefitEntitlementBffRequest(
    long      ProviderProfileId,
    long      BenefitRuleId,
    DateTime  GrantedFrom,
    DateTime? GrantedTo,
    long?     UsageLimit,
    decimal?  MaximumEligibleGMV);

public sealed record GrantEntitlementBffResult(long Id, string EntitlementCode);
public sealed record RevokeEntitlementBffResult(long Id, string? EntitlementCode);

/// <summary>Resolved effective commission (preview) — base rate + applied benefit adjustments.</summary>
public sealed record EffectiveCommissionResolveBffResult(
    string        BaseRuleCode,
    decimal       BaseRate,
    List<string>  AppliedBenefitRuleCodes,
    decimal       RequestedAdjustment,
    decimal       AppliedAdjustment,
    decimal       EffectiveCommissionRate,
    decimal       RequestedBenefitAmount,
    decimal       AppliedBenefitAmount,
    decimal       BenefitedServiceAmount,
    decimal       NonBenefitedServiceAmount,
    string?       AdjustmentReason,
    List<long>    AppliedBenefitRuleIds);
