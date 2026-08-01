using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Dto;

/// <summary>
/// Application-layer DTO for platform fee rule admin management (BE-P3). Mirrors the CommissionRule admin surface.
/// All enum fields are passed as their C# enum types — the Payment module API uses StringEnumConverter, so they
/// serialize as strings to any upstream caller (the BFF Newtonsoft contract reads them as strings). SpecificityRank
/// is the CustomerType+Category(4) &gt; CustomerType(3) &gt; Category(2) &gt; Global(1) rank the resolver assigns.
/// </summary>
public sealed record PlatformFeeRuleDto(
    long                   Id,
    string?                RuleCode,
    PlatformFeeModel       Model,
    decimal?               Rate,
    decimal?               FixedAmount,
    decimal?               MinAmount,
    decimal?               MaxAmount,
    string                 CurrencyCode,
    string?                CategoryCode,
    string?                CustomerType,
    CommissionRulePriority Priority,
    DateTime               EffectiveFrom,
    DateTime?              EffectiveTo,
    CommissionRuleStatus   Status,
    bool                   IsActive,
    string?                RuleName,
    string?                Notes,
    decimal?               VatRate,
    int                    SpecificityRank,
    long?                  CreateUserId,
    DateTime?              CreateDate,
    long?                  ModifyUserId,
    DateTime?              ModifyDate
);

/// <summary>Paged platform fee rules list response.</summary>
public sealed record PlatformFeeRuleListResult(
    List<PlatformFeeRuleDto> Items,
    int                      Total,
    int                      Page,
    int                      PageSize
);

/// <summary>KPI strip DTO for the admin platform fee rules dashboard.</summary>
public sealed record PlatformFeeRuleStatsDto(
    int TotalRules,
    int ActiveRules,
    int PercentageRules,
    int FixedRules,
    int BoundsRules,
    int WaivedRules
);
