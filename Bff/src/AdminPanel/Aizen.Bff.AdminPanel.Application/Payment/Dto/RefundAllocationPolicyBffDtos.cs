namespace Aizen.Bff.AdminPanel.Application.Payment.Dto;

// ─── P10 RefundAllocationPolicy — BFF REQUEST DTOs ────────────────────────────
// Enum-typed fields are `string` (BFF Newtonsoft has no StringEnumConverter; the Payment
// module's JSON deserializer accepts string enum names on the request path).
// Response/read DTOs live in Aizen.Modules.Payment.Abstraction.Dto and are reused directly.

/// <summary>One allocation rule input. Cause/Mode are string enum names parsed by the module.</summary>
public sealed record RefundAllocationPolicyRuleBffInput(
    string   Cause,
    string   Mode,
    decimal? FixedPlatformFeeAmount);

/// <summary>Create a refund-allocation policy with optional per-cause rules.</summary>
public sealed record CreateRefundAllocationPolicyBffRequest(
    string    CurrencyCode,
    decimal   NegativeBalanceLimit,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   PolicyName,
    string?   Notes,
    List<RefundAllocationPolicyRuleBffInput>? Rules);

/// <summary>Update a refund-allocation policy. Id is taken from the route by the module controller.</summary>
public sealed record UpdateRefundAllocationPolicyBffRequest(
    decimal   NegativeBalanceLimit,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   Notes);
