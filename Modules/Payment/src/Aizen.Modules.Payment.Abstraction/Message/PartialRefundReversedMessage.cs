using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// Published when a TransactionRefundRecord is reversed (funds returned to platform).
///
/// Consumer (ServiceRequest module / Notification module):
///   - Update SR financial summary
///   - Notify the payer that their refund was reversed
/// </summary>
public sealed class PartialRefundReversedMessage : AizenBaseMessage
{
    public long                    TransactionId   { get; init; }
    public string                  TransactionCode { get; init; } = default!;
    public long                    RefundRecordId  { get; init; }
    public string                  RefundCode      { get; init; } = default!;
    public TransactionContextType  ContextType     { get; init; }
    public long                    ContextId       { get; init; }
    public long                    PayerProfileId  { get; init; }
    public decimal                 ReversedAmount  { get; init; }
    public string                  CurrencyCode    { get; init; } = "TRY";
    public string                  ReversalReason  { get; init; } = default!;
    public DateTime                ReversedAtUtc   { get; init; }
}
