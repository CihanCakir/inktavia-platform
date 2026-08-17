namespace Aizen.Bff.AdminPanel.Application.Payment.Dto;

// ─── Commission Rule — BFF DTOs ───────────────────────────────────────────────
//
// IMPORTANT: All enum-typed fields MUST be declared as `string` here.
// AizenBffServiceConfiguration uses Newtonsoft.Json WITHOUT StringEnumConverter,
// so enum values arrive as integers from the Payment module's JSON serializer
// and would be de-serialized incorrectly if typed as C# enums in BFF DTOs.
// Use string everywhere and let the frontend parse accordingly.

/// <summary>
/// Single commission rule detail — returned by GetById and list items.
/// Phase 13 (July 2026): Added CargoDry targeting (ContextType, ProductCode, SalesChannel)
/// and labeling fields (RuleName, CurrencyCode, CommercialModel). All enum-typed at source
/// are represented as string? here per BFF JSON serialization contract.
/// </summary>
public sealed record CommissionRuleBffDto(
    long      Id,
    string?   RuleCode,
    string    RuleType,       // "Global" | "Category" | "Plan" | "ProviderOverride"
    string?   CategoryCode,
    long?     ProviderPlanId,
    long?     ProviderProfileId,
    decimal   CommissionRate,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   Notes,
    string    Status,         // "Draft" | "Active" | "Scheduled" | "Expired" | "Inactive"
    string    Priority,       // "Low" | "Standard" | "Medium" | "High" | "EMERGENCY"
    long      ResolvedAppliedCount,
    bool      IsActive,
    string?   CreatedByAdminId,
    DateTime? CreateDate,
    string?   UpdatedByAdminId,
    DateTime? ModifyDate,
    // ── Phase 0: CargoDry targeting dimensions ────────────────────────────────
    string?   ContextType,    // null = all contexts; "CargoDry" | "ServiceRequest" | ...
    string?   ProductCode,    // null = all products
    string?   SalesChannel,   // null = all channels; "Direct" | "CargoDryKit" | ...
    // ── Phase 5: Rule labeling / metadata ────────────────────────────────────
    string?   RuleName,       // human-readable label
    string?   CurrencyCode,   // null = any currency
    string?   CommercialModel // null = any commercial model; "B2B" | "B2C" | ...
)
{
    // BFF enrichment — resolved from Identity module for ProviderOverride rules
    public string? ProviderDisplayName { get; init; }

    // BFF enrichment — resolved from Plans for Plan rules
    public string? PlanName { get; init; }
}

/// <summary>
/// Paged list response for commission rules.
/// </summary>
public sealed record CommissionRuleListBffResult(
    List<CommissionRuleBffDto> Items,
    int                        Total,
    int                        Page,
    int                        PageSize
);

/// <summary>
/// KPI stats for the admin commission rules dashboard strip.
/// </summary>
public sealed record CommissionRuleStatsBffDto(
    int     TotalRules,
    int     ActiveRules,
    int     EmergencyRules,
    decimal GlobalBaseRate,
    int     ScheduledRules,
    int     DraftRules
);

// ─── Commission Rule — BFF Request bodies ────────────────────────────────────

/// <summary>
/// Request body for creating a new commission rule.
/// Phase 13 (July 2026): Added CargoDry targeting and labeling fields.
/// </summary>
public sealed record CreateCommissionRuleBffRequest(
    string    RuleType,        // "Global" | "Category" | "Plan" | "ProviderOverride"
    string?   CategoryCode,
    long?     ProviderPlanId,
    long?     ProviderProfileId,
    decimal   CommissionRate,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   Notes,
    string    Priority,        // "Low" | "Standard" | "Medium" | "High" | "EMERGENCY"
    // ── Phase 0: CargoDry targeting dimensions ────────────────────────────────
    string?   ContextType    = null,  // "CargoDry" | "ServiceRequest" | null = all
    string?   ProductCode    = null,
    string?   SalesChannel   = null,  // "Direct" | "CargoDryKit" | null = all
    // ── Phase 5: Rule labeling / metadata ────────────────────────────────────
    string?   RuleName       = null,
    string?   CurrencyCode   = null,
    string?   CommercialModel = null  // "B2B" | "B2C" | null = any
);

/// <summary>Request body for updating an existing commission rule.</summary>
public sealed record UpdateCommissionRuleBffRequest(
    decimal   CommissionRate,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string    Priority,
    string?   Notes
);

/// <summary>Result returned from create commission rule.</summary>
public sealed record CommissionRuleCreateBffResult(long Id, string RuleCode);

/// <summary>Result returned from update and deactivate.</summary>
public sealed record CommissionRuleMutateBffResult(long Id, string? RuleCode);
