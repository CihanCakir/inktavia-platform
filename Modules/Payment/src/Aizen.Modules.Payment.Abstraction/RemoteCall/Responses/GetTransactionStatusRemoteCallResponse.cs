namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

/// <summary>
/// BE-MO3 — a lean, cost-free read of a payment transaction's lifecycle state, for an owner "did my payment go
/// through?" poll. Deliberately a NARROW projection of the full transaction: it carries the customer-facing
/// gross (what the owner paid) and the status/timestamps only — <b>never</b> commission / net-payout / discount
/// funding (those never cross this seam). Reuses the existing read; no capture/webhook logic here.
/// </summary>
public sealed class GetTransactionStatusRemoteCallResponse
{
    public required long   TransactionId { get; init; }
    /// <summary>The <c>PaymentTransactionStatus</c> name (e.g. PendingIntent, Captured, Failed, Cancelled).</summary>
    public required string Status        { get; init; }
    /// <summary>The numeric status code, for a stable machine-readable check across the wire.</summary>
    public required int    StatusCode    { get; init; }
    /// <summary>Customer total — what the owner pays. Cost-free (the customer-facing amount, not provider economics).</summary>
    public required decimal GrossAmount  { get; init; }
    public required string CurrencyCode  { get; init; }
    public bool     EscrowRequired { get; init; }
    /// <summary>The context that triggered the transaction (e.g. ServiceRequestOffer) — for a defensive cross-check.</summary>
    public string?  ContextType   { get; init; }
    public long     ContextId     { get; init; }
    public long?    ContextSubId  { get; init; }
    public long     PayerProfileId { get; init; }
    public DateTime? CapturedAt   { get; init; }
    public DateTime? CancelledAt  { get; init; }
}
