using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// Published when a payment is refunded (full or partial).
/// Consumers: Notification module, Accounting/Reporting module (post-MVP).
/// </summary>
public sealed class PaymentRefundedMessage : AizenBaseMessage
{
    public long   TransactionId     { get; init; }
    public string TransactionCode   { get; init; } = default!;

    public TransactionContextType ContextType  { get; init; }
    public long                   ContextId    { get; init; }
    public long?                  ContextSubId { get; init; }

    public long    PayerProfileId   { get; init; }

    public decimal RefundedAmount   { get; init; }
    public decimal OriginalAmount   { get; init; }
    public string  CurrencyCode     { get; init; } = "TRY";

    /// <summary>True when RefundedAmount &lt; OriginalAmount.</summary>
    public bool    IsPartial        { get; init; }

    public string  Reason           { get; init; } = default!;
    public DateTime RefundedAtUtc   { get; init; }
}
