using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// Published when a payment has been successfully captured by the gateway.
/// Consumers: ServiceRequest module (to optionally advance SR state), Notification module.
/// </summary>
public sealed class PaymentCapturedMessage : AizenBaseMessage
{
    /// <summary>Internal transaction ID.</summary>
    public long   TransactionId       { get; init; }
    public string TransactionCode     { get; init; } = default!;
    public string GatewayReference    { get; init; } = default!;

    /// <summary>Context linking this payment to its business entity (SR, CargoDry, Subscription).</summary>
    public TransactionContextType ContextType  { get; init; }
    public long                   ContextId    { get; init; }
    public long?                  ContextSubId { get; init; }

    public long    PayerProfileId     { get; init; }
    public long?   RecipientProfileId { get; init; }

    public decimal GrossAmount        { get; init; }
    public decimal NetPayoutAmount    { get; init; }
    public decimal CommissionAmount   { get; init; }
    public string  CurrencyCode       { get; init; } = "TRY";

    public DateTime CapturedAtUtc     { get; init; }
}
