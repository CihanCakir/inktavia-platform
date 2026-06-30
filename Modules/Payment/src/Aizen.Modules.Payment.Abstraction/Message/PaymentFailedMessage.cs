using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// Published when a payment fails or times out. The SR module MUST NOT stop the case
/// immediately on receipt — it should set the SR to AwaitingPayment and allow retry.
/// Only after admin-configured retry grace period (e.g., 48h) should the SR be cancelled.
/// </summary>
public sealed class PaymentFailedMessage : AizenBaseMessage
{
    public long   TransactionId    { get; init; }
    public string TransactionCode  { get; init; } = default!;

    public TransactionContextType ContextType  { get; init; }
    public long                   ContextId    { get; init; }
    public long?                  ContextSubId { get; init; }

    public long   PayerProfileId   { get; init; }

    public decimal GrossAmount     { get; init; }
    public string  CurrencyCode    { get; init; } = "TRY";

    /// <summary>Reason: "timeout" | "gateway_failure" | "webhook_failure"</summary>
    public string  FailureReason   { get; init; } = default!;

    /// <summary>Indicates this transaction may still be retried by the payer.</summary>
    public bool    IsRetryable     { get; init; } = true;

    public DateTime FailedAtUtc    { get; init; }
}
