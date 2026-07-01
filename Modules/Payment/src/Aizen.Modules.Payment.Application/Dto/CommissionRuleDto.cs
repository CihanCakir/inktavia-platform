using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Dto;

/// <summary>
/// Application-layer DTO for commission rule admin management.
/// All enum fields are passed as their C# enum types — the Payment module API uses StringEnumConverter,
/// so they serialize as strings to any upstream caller.
/// </summary>
public sealed record CommissionRuleDto(
    long                   Id,
    string?                RuleCode,
    CommissionRuleType     RuleType,
    string?                CategoryCode,
    long?                  ProviderPlanId,
    long?                  ProviderProfileId,
    decimal                CommissionRate,
    DateTime               EffectiveFrom,
    DateTime?              EffectiveTo,
    string?                Notes,
    CommissionRuleStatus   Status,
    CommissionRulePriority Priority,
    long                   ResolvedAppliedCount,
    bool                   IsActive,
    long?                  CreateUserId,
    DateTime?              CreateDate,
    long?                  ModifyUserId,
    DateTime?              ModifyDate
);

/// <summary>Paged commission rules list response.</summary>
public sealed record CommissionRuleListResult(
    List<CommissionRuleDto> Items,
    int                     Total,
    int                     Page,
    int                     PageSize
);

/// <summary>KPI strip DTO for admin commission rules dashboard.</summary>
public sealed record CommissionRuleStatsDto(
    int     TotalRules,
    int     ActiveRules,
    int     EmergencyRules,
    decimal GlobalBaseRate,
    int     ScheduledRules,
    int     DraftRules
);
