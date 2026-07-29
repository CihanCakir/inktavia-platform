namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;

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
