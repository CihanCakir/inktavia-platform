namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;

// ─── P10 ProviderBalance ledger — BFF REQUEST DTOs ────────────────────────────
// Response/read DTOs live in Aizen.Modules.Payment.Abstraction.Dto and are reused directly.

/// <summary>Manual signed adjustment to a provider's balance ledger.</summary>
public sealed record AdjustProviderBalanceBffRequest(
    string  CurrencyCode,
    decimal SignedAmount,
    long    AdminUserId,
    string  Note);
