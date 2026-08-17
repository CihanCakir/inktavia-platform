using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// Published by PaymentReminderJob when a transaction remains in PendingIntent.
/// The Notification module should send a payment reminder to the payer.
/// </summary>
public sealed class PaymentReminderRequestedMessage : AizenBaseMessage
{
    public long   TransactionId    { get; init; }
    public string TransactionCode  { get; init; } = default!;

    public TransactionContextType ContextType  { get; init; }
    public long                   ContextId    { get; init; }

    public long   PayerProfileId   { get; init; }
    public decimal GrossAmount     { get; init; }
    public string  CurrencyCode    { get; init; } = "TRY";

    /// <summary>How long the transaction has been pending, e.g. "1h", "24h".</summary>
    public string  ReminderWindow  { get; init; } = default!;

    public DateTime PendingSinceUtc { get; init; }
}
