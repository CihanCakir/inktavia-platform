using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Dto;

/// <summary>
/// Application-layer DTO for profit-protection policy admin management (BE-P5). Mirrors the P3 platform-fee admin DTO
/// shape (used for both list and detail). All enum fields are C# enum types — the Payment module API's
/// StringEnumConverter serializes them as strings to upstream callers. Unlike the rule DTOs there is no
/// specificity/priority: profit-protection is a single-active-per-currency versioned policy.
/// </summary>
public sealed record ProfitProtectionPolicyDto(
    long                            Id,
    string?                         PolicyCode,
    string                          CurrencyCode,
    // ── Minimum contribution gates (amount + rate; Required = Max(amount, base × rate)) ──
    decimal                         MinCustomerSideContributionAmount,
    decimal                         MinCustomerSideContributionRate,
    decimal                         MinProviderSideContributionAmount,
    decimal                         MinProviderSideContributionRate,
    decimal                         MinTransactionContributionAmount,
    decimal                         MinTransactionContributionRate,
    // ── Expected variable expenses ──────────────────────────────────────────────
    decimal                         PaymentProcessingExpenseRate,
    decimal                         PaymentProcessingFixed,
    decimal                         RefundRiskReserveRate,
    decimal                         OtherVariableExpenseRate,
    decimal                         OtherVariableExpenseFixed,
    // ── Variable-cost split + adjustment order ──────────────────────────────────
    decimal                         CustomerSideVariableCostShareRate,
    // ── BE-S9 line-level defaults (§20.12; admin-tunable) ────────────────────────
    decimal                         DefaultLineMinProviderReceivableRate,
    decimal                         DefaultLineMinProviderReceivableAmount,
    decimal                         DefaultAllowedProviderFundedDiscountRate,
    decimal                         DefaultAllowedPlatformFundedDiscountRate,
    decimal                         LineCommissionFloorRate,
    decimal                         MinLinePlatformContributionRate,
    bool                            StrategicLossExceptionEnabled,
    decimal                         StrategicLossExceptionMaxLineDeficit,
    ProfitProtectionAdjustmentOrder AdjustmentOrder,
    // ── Lifecycle / admin ───────────────────────────────────────────────────────
    DateTime                        EffectiveFrom,
    DateTime?                       EffectiveTo,
    CommissionRuleStatus            Status,
    bool                            IsActive,
    string?                         PolicyName,
    string?                         Notes,
    long?                           CreateUserId,
    DateTime?                       CreateDate,
    long?                           ModifyUserId,
    DateTime?                       ModifyDate
);

/// <summary>Profit-protection policy version-history list (no paging — policies are few).</summary>
public sealed record ProfitProtectionPolicyListResult(
    List<ProfitProtectionPolicyDto> Items,
    int                             Total
);
