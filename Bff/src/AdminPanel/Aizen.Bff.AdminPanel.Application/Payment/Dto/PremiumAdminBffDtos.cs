namespace Aizen.Bff.AdminPanel.Application.Payment.Dto;

// ─── P11 Premium admin — BFF REQUEST DTOs ─────────────────────────────────────
// Enum-typed fields are `string` (BFF Newtonsoft has no StringEnumConverter; the Payment
// module's JSON deserializer accepts string enum names on the request path).
// Response/read DTOs live in Aizen.Modules.Payment.Abstraction.Dto and are reused directly.

/// <summary>Create a premium product. EntitlementType is a string enum name parsed by the module.</summary>
public sealed record CreatePremiumProductBffRequest(
    string  Code,
    string  Name,
    string  EntitlementType,
    int     DurationDays,
    string? Description);

/// <summary>Update a premium product. Id is taken from the route by the module controller.</summary>
public sealed record UpdatePremiumProductBffRequest(
    string  Name,
    int     DurationDays,
    string? Description);

/// <summary>Create a versioned premium product price.</summary>
public sealed record CreatePremiumProductPriceBffRequest(
    long      PremiumProductId,
    decimal   PriceAmount,
    string    CurrencyCode,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   Notes);

/// <summary>Update a premium product price. Id is taken from the route by the module controller.</summary>
public sealed record UpdatePremiumProductPriceBffRequest(
    decimal   PriceAmount,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   Notes);
