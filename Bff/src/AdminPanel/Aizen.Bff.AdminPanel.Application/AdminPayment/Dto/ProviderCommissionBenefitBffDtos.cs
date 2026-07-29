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
