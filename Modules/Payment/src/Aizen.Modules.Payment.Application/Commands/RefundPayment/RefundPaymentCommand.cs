using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.RefundPayment;

/// <summary>
/// Issues a full or partial gateway refund for a captured/released transaction.
///
/// Use for:
///   - Full refund triggered by ServiceRequest cancellation (from ServiceRequestCancelledConsumer)
///   - Admin-initiated partial or full refund from admin panel
///
/// After successful gateway confirmation the handler creates a TransactionRefundRecord
/// and calls tx.ApplyRefund() to update TotalRefundedAmount and transaction status.
/// </summary>
public sealed class RefundPaymentCommand : AizenCommand<RefundPaymentResult>
{
    public required long         TransactionId { get; init; }
    public required decimal      RefundAmount  { get; init; }
    public required RefundReason Reason        { get; init; }
    public required RefundType   RefundType    { get; init; }
    public          string?      AdminNote     { get; init; }
}
