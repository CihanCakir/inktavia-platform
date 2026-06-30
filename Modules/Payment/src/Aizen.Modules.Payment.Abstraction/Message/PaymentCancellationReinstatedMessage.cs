using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// Published when a previously cancelled transaction is reinstated to PendingIntent.
///
/// Consumer (ServiceRequest module): restore the SR to its pre-cancellation state
/// so the payer can initiate a new checkout for the same request.
/// </summary>
public sealed class PaymentCancellationReinstatedMessage : AizenBaseMessage
{
    public long                    TransactionId   { get; init; }
    public string                  TransactionCode { get; init; } = default!;
    public TransactionContextType  ContextType     { get; init; }
    public long                    ContextId       { get; init; }
    public long?                   ContextSubId    { get; init; }
    public long                    PayerProfileId  { get; init; }
    public decimal                 GrossAmount     { get; init; }
    public string                  CurrencyCode    { get; init; } = "TRY";
    public string                  AdminNote       { get; init; } = default!;
    public DateTime                ReinstatedAtUtc { get; init; }
}
