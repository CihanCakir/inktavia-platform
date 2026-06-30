using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Model;

/// <summary>
/// Input model for gateway checkout/escrow initiation.
/// Passed by CreatePaymentEscrowCommandHandler to IPaymentGatewayProvider.InitiateCheckoutAsync.
/// </summary>
public sealed class CheckoutInitInput
{
    /// <summary>Internal transaction ID (already persisted before gateway call).</summary>
    public required long   TransactionId      { get; init; }

    /// <summary>Globally unique idempotency key — e.g. "SR-{srId}-OFFER-{offerId}".</summary>
    public required string IdempotencyKey     { get; init; }

    /// <summary>Full amount charged to the payer (before any splits).</summary>
    public required decimal GrossAmount       { get; init; }

    public required string  CurrencyCode      { get; init; }  // e.g. "TRY"

    /// <summary>Platform user ID of the payer. MVP: same as participant userId.</summary>
    public required long    PayerProfileId    { get; init; }

    /// <summary>Provider profile ID — used to look up sub-merchant key for Iyzico.</summary>
    public long?            RecipientProfileId { get; init; }

    /// <summary>Pre-resolved Iyzico sub-merchant key; null for manual gateway.</summary>
    public string?          SubMerchantKey    { get; init; }

    /// <summary>Net amount the provider receives after commission. Used for Iyzico basket split.</summary>
    public decimal          ProviderNetAmount  { get; init; }

    /// <summary>Transaction context (type + SR id + offer id).</summary>
    public required TransactionContext Context { get; init; }

    /// <summary>true = hold in escrow until admin releases; false = immediate settle.</summary>
    public bool             EscrowRequired    { get; init; } = true;

    /// <summary>Human-readable payment description shown on the payment form.</summary>
    public string?          Description       { get; init; }

    /// <summary>Redirect after successful Iyzico checkout.</summary>
    public string?          ReturnUrl         { get; init; }

    /// <summary>Redirect on cancel.</summary>
    public string?          CancelUrl         { get; init; }
}
