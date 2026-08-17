using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Dto;

/// <summary>
/// Application-layer DTO for commission rule admin management.
/// All enum fields are passed as their C# enum types — the Payment module API uses StringEnumConverter,
/// so they serialize as strings to any upstream caller.
/// Phase 0 (July 2026): Added ContextType, ProductCode, SalesChannel for CargoDry channel-specific rates.
/// Phase 5 (July 2026): Added RuleName, CurrencyCode, CommercialModel for rule labeling and multi-market scope.
/// Phase 13 (July 2026): All Phase 5 fields now surfaced in DTO (were in entity but not mapped).
/// </summary>
public sealed record CommissionRuleDto(
    long                    Id,
    string?                 RuleCode,
    CommissionRuleType      RuleType,
    string?                 CategoryCode,
    long?                   ProviderPlanId,
    long?                   ProviderProfileId,
    decimal                 CommissionRate,
    DateTime                EffectiveFrom,
    DateTime?               EffectiveTo,
    string?                 Notes,
    CommissionRuleStatus    Status,
    CommissionRulePriority  Priority,
    long                    ResolvedAppliedCount,
    bool                    IsActive,
    long?                   CreateUserId,
    DateTime?               CreateDate,
    long?                   ModifyUserId,
    DateTime?               ModifyDate,
    // ── Phase 0: CargoDry targeting dimensions ──────────────────────────────
    TransactionContextType? ContextType,    // null = all contexts
    string?                 ProductCode,    // null = all products
    SalesChannel?           SalesChannel,   // null = all channels
    // ── Phase 5: Rule labeling / metadata ───────────────────────────────────
    string?                 RuleName,       // human-readable label for trace and admin UI
    string?                 CurrencyCode,   // null = any currency
    CommercialModel?        CommercialModel  // null = any commercial model
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
