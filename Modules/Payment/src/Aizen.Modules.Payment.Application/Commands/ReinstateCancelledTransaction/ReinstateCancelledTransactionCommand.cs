using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.ReinstateCancelledTransaction;

/// <summary>
/// Restores a Cancelled transaction back to PendingIntent.
///
/// Use when a cancellation was made in error and the payer still needs to pay.
/// After reinstatement a new checkout must be initiated — this command only
/// resets the transaction state. The original CancelledAt / CancellationReason
/// are preserved for audit purposes.
/// </summary>
public sealed class ReinstateCancelledTransactionCommand : AizenCommand<ReinstateCancelledTransactionResult>
{
    public required long   TransactionId { get; init; }
    public required string AdminNote     { get; init; }
}
