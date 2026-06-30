using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.ReversePartialRefund;

/// <summary>
/// Reverses a previously processed refund record, returning the funds to the platform.
///
/// Only works when:
///   - The TransactionRefundRecord.Status == Processed
///   - Bank settlement has not yet occurred
///
/// After reversal:
///   - RefundRecord.Status → Reversed (audit trail preserved, record not deleted)
///   - Transaction.TotalRefundedAmount is reduced by the refund amount
///   - Transaction.Status is recalculated (PartiallyRefunded | Captured | Released)
/// </summary>
public sealed class ReversePartialRefundCommand : AizenCommand<ReversePartialRefundResult>
{
    public required long   RefundRecordId  { get; init; }
    public required string ReversalReason  { get; init; }
    public          string? AdminNote      { get; init; }
}
