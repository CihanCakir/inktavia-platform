using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// Published when a PendingIntent transaction is cancelled before any money is captured.
///
/// Consumer (ServiceRequest module): move SR to AwaitingPayment or Cancelled
/// based on the CancellationReason.
/// </summary>
public sealed class PaymentCancelledMessage : AizenBaseMessage
{
    public long                    TransactionId      { get; init; }
    public string                  TransactionCode    { get; init; } = default!;
    public TransactionContextType  ContextType        { get; init; }
    public long                    ContextId          { get; init; }
    public long?                   ContextSubId       { get; init; }
    public long                    PayerProfileId     { get; init; }
    public decimal                 GrossAmount        { get; init; }
    public string                  CurrencyCode       { get; init; } = "TRY";
    public string                  CancellationReason { get; init; } = default!;
    public DateTime                CancelledAtUtc     { get; init; }
}
