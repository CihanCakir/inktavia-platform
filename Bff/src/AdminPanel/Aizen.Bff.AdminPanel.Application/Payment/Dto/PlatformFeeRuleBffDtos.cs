namespace Aizen.Bff.AdminPanel.Application.Payment.Dto;

// ─── BE-P3 PlatformFeeRule — BFF DTOs ─────────────────────────────────────────
// Enum-typed fields are `string` (BFF Newtonsoft has no StringEnumConverter; the Payment module's
// JSON deserializer accepts string enum names on the request path).

/// <summary>Create a platform-fee rule. Model = Percentage | Fixed | PercentageWithBounds | Waived.</summary>
public sealed record CreatePlatformFeeRuleBffRequest(
    string    Model,
    decimal?  Rate,
    decimal?  FixedAmount,
    decimal?  MinAmount,
    decimal?  MaxAmount,
    string    CurrencyCode,
    string?   CategoryCode,
    string?   CustomerType,
    string    Priority,          // Low | Standard | Medium | High | EMERGENCY
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   RuleName,
    string?   Notes,
    decimal?  VatRate);

/// <summary>Update a platform-fee rule. <c>Id</c> is forced from the route by the handler so route and body always match.</summary>
public sealed record UpdatePlatformFeeRuleBffRequest(
    long      Id,
    string    Model,
    decimal?  Rate,
    decimal?  FixedAmount,
    decimal?  MinAmount,
    decimal?  MaxAmount,
    string    Priority,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   RuleName,
    string?   Notes,
    decimal?  VatRate);

public sealed record PlatformFeeRuleCreateBffResult(long Id, string RuleCode);
public sealed record PlatformFeeRuleMutateBffResult(long Id, string? RuleCode);

// ─── List / Detail / Stats (BE-P3 admin surface) ──────────────────────────────
// Enum-typed source fields (Model/Priority/Status) are `string` per the BFF Newtonsoft contract — the Payment
// module serializes enums as their string names, so these arrive as e.g. "Percentage" / "Standard" / "Active".

/// <summary>A platform fee rule as it appears in the admin list.</summary>
public sealed record PlatformFeeRuleListItemBffDto(
    long      Id,
    string?   RuleCode,
    string    Model,            // "Percentage" | "Fixed" | "PercentageWithBounds" | "Waived"
    decimal?  Rate,
    decimal?  FixedAmount,
    decimal?  MinAmount,
    decimal?  MaxAmount,
    string    CurrencyCode,
    string?   CategoryCode,
    string?   CustomerType,
    string    Priority,         // "Low" | "Standard" | "Medium" | "High" | "EMERGENCY"
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string    Status,           // "Active" | "Scheduled" | "Expired" | "Inactive"
    bool      IsActive,
    string?   RuleName,
    string?   Notes,
    decimal?  VatRate,
    int       SpecificityRank,  // CustomerType+Category(4) > CustomerType(3) > Category(2) > Global(1)
    long?     CreateUserId,
    DateTime? CreateDate,
    long?     ModifyUserId,
    DateTime? ModifyDate
);

/// <summary>Paged list response for platform fee rules.</summary>
public sealed record PlatformFeeRulesListBffResult(
    List<PlatformFeeRuleListItemBffDto> Items,
    int                                 Total,
    int                                 Page,
    int                                 PageSize
);

/// <summary>Full detail of a single platform fee rule (same shape as the list item at MVP).</summary>
public sealed record PlatformFeeRuleDetailBffDto(
    long      Id,
    string?   RuleCode,
    string    Model,
    decimal?  Rate,
    decimal?  FixedAmount,
    decimal?  MinAmount,
    decimal?  MaxAmount,
    string    CurrencyCode,
    string?   CategoryCode,
    string?   CustomerType,
    string    Priority,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string    Status,
    bool      IsActive,
    string?   RuleName,
    string?   Notes,
    decimal?  VatRate,
    int       SpecificityRank,
    long?     CreateUserId,
    DateTime? CreateDate,
    long?     ModifyUserId,
    DateTime? ModifyDate
);

/// <summary>KPI stats for the admin platform fee rules dashboard strip.</summary>
public sealed record PlatformFeeRuleStatsBffDto(
    int TotalRules,
    int ActiveRules,
    int PercentageRules,
    int FixedRules,
    int BoundsRules,
    int WaivedRules
);

/// <summary>Point-in-time platform-fee resolution (dev/preview). Model/VatSource are strings per the BFF enum contract.</summary>
public sealed record PlatformFeeResolveBffResult(
    long     RuleId,
    string?  RuleCode,
    string   Model,
    decimal? Rate,
    decimal? MinAmount,
    decimal? MaxAmount,
    decimal? FixedAmount,
    int      SpecificityRank,
    string   Source,
    decimal  FeeNet,
    decimal  FeeVat,
    decimal  FeeGross,
    decimal  VatRate,
    string   VatSource);
