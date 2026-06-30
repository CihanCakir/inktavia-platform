using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.CancelPayment;

/// <summary>
/// Cancels a PendingIntent transaction before any money is captured.
/// No gateway call is required — the intent is voided in-system only.
///
/// For Captured transactions a full refund should be issued via RefundPaymentCommand.
/// </summary>
public sealed class CancelPaymentCommand : AizenCommand<CancelPaymentResult>
{
    public required long               TransactionId      { get; init; }
    public required CancellationReason CancellationReason { get; init; }
    public          string?            AdminNote          { get; init; }
}
