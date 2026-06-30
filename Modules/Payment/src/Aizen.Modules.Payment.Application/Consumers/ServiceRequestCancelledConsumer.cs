using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Commands.RefundPayment;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Consumers;

/// <summary>
/// Listens for ServiceRequestCancelledMessage from the ServiceRequest module.
/// Triggers a full refund when the payment is in Captured (escrow) state.
///
/// State matrix:
///   PendingIntent / Cancelled  → no-op (nothing charged yet)
///   Captured                   → full refund via RefundPaymentCommand
///   Released                   → no-op (funds transferred; requires manual dispute)
///   Refunded / PartialRefunded → no-op (already refunded)
/// </summary>
public sealed class ServiceRequestCancelledConsumer
    : AizenBaseMessageConsumer<ServiceRequestCancelledMessage>
{
    private readonly ISender                                             _sender;
    private readonly IPaymentTransactionRepository                       _transactions;
    private readonly ILogger<ServiceRequestCancelledConsumer>            _logger;

    public ServiceRequestCancelledConsumer(IServiceProvider sp) : base(sp)
    {
        _sender       = sp.GetRequiredService<ISender>();
        _transactions = sp.GetRequiredService<IPaymentTransactionRepository>();
        _logger       = sp.GetRequiredService<ILogger<ServiceRequestCancelledConsumer>>();
    }

    public override async Task<bool> ExecutePrepareMessage(
        ServiceRequestCancelledMessage message, CancellationToken ct)
    {
        var tx = await _transactions.GetByContextAsync(
            TransactionContextType.ServiceRequest, message.ServiceRequestId, ct);

        if (tx is null)
        {
            _logger.LogInformation(
                "ServiceRequestCancelledConsumer: no transaction for SR {SRId}. Nothing to refund.",
                message.ServiceRequestId);
            return false;
        }

        if (tx.Status != PaymentTransactionStatus.Captured)
        {
            _logger.LogInformation(
                "ServiceRequestCancelledConsumer: SR {SRId} transaction {TxId} in status {Status} — no refund action.",
                message.ServiceRequestId, tx.Id, tx.Status);
            return false;
        }

        return true;
    }

    public override async Task ExecuteCommitMessage(
        ServiceRequestCancelledMessage message, CancellationToken ct)
    {
        var tx = await _transactions.GetByContextAsync(
            TransactionContextType.ServiceRequest, message.ServiceRequestId, ct);

        if (tx is null || tx.Status != PaymentTransactionStatus.Captured)
            return;

        _logger.LogInformation(
            "ServiceRequestCancelledConsumer: triggering full refund for SR {SRId} → Tx {TxId} Amount {Amount}",
            message.ServiceRequestId, tx.Id, tx.GrossAmount);

        await _sender.Send(new RefundPaymentCommand
        {
            TransactionId = tx.Id,
            RefundAmount  = tx.GrossAmount,
            Reason        = RefundReason.ServiceRequestCancelled,
            RefundType    = RefundType.Full,
            AdminNote     = $"SR {message.ServiceRequestId} cancelled on {message.CancelledAtUtc:u}. Auto-refund.",
        }, ct);
    }

    public override Task ExecuteRollbackMessage(
        ServiceRequestCancelledMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogError(
            "ServiceRequestCancelledConsumer rollback for SR {SRId}: {Error}",
            message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
