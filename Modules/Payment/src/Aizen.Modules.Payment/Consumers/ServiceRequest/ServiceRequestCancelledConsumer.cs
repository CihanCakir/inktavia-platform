using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Consumers.ServiceRequest;

/// <summary>
/// Listens for ServiceRequestCancelledMessage from the ServiceRequest module.
/// Triggers a full gateway refund when the transaction is in Captured (escrow) state.
///
/// State matrix:
///   PendingIntent / Cancelled  → no-op (nothing charged yet)
///   Captured                   → full gateway refund + publishes PaymentRefundedMessage
///   Released                   → no-op (funds transferred; requires manual dispute)
///   Refunded / PartialRefunded → no-op (already refunded)
/// </summary>
public sealed class ServiceRequestCancelledConsumer
    : AizenBaseMessageConsumer<ServiceRequestCancelledMessage>
{
    private readonly IPaymentTransactionRepository            _transactions;
    private readonly PaymentGatewayResolver                   _gatewayResolver;
    private readonly IAizenMessagePublisher                   _publisher;
    private readonly ILogger<ServiceRequestCancelledConsumer> _logger;

    public ServiceRequestCancelledConsumer(IServiceProvider sp) : base(sp)
    {
        _transactions    = sp.GetRequiredService<IPaymentTransactionRepository>();
        _gatewayResolver = sp.GetRequiredService<PaymentGatewayResolver>();
        _publisher       = sp.GetRequiredService<IAizenMessagePublisher>();
        _logger          = sp.GetRequiredService<ILogger<ServiceRequestCancelledConsumer>>();
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
                "ServiceRequestCancelledConsumer: SR {SRId} Tx {TxId} is {Status} — no refund action.",
                message.ServiceRequestId, tx.Id, tx.Status);
            return false;
        }

        return true;
    }

    public override async Task ExecuteCommitMessage(
        ServiceRequestCancelledMessage message, CancellationToken ct)
    {
        // Reload with refund records so ApplyRefund domain guard checks can run
        var tx = await _transactions.GetByContextAsync(
            TransactionContextType.ServiceRequest, message.ServiceRequestId, ct);

        if (tx is null || tx.Status != PaymentTransactionStatus.Captured) return;

        _logger.LogInformation(
            "ServiceRequestCancelledConsumer: triggering full refund for SR {SRId} → Tx {TxId} Amount {Amount}",
            message.ServiceRequestId, tx.Id, tx.GrossAmount);

        // ── Gateway call ──────────────────────────────────────────────────────
        var gateway = _gatewayResolver.Resolve();
        var gatewayResult = await gateway.RefundAsync(new RefundInput
        {
            TransactionId    = tx.Id,
            GatewayReference = tx.GatewayReference ?? string.Empty,
            RefundAmount     = tx.GrossAmount,
            Currency         = tx.CurrencyCode,
            Reason           = RefundReason.ServiceRequestCancelled,
            AdminNote        = $"SR {message.ServiceRequestId} cancelled on {message.CancelledAtUtc:u}. Auto-refund.",
        }, ct);

        if (!gatewayResult.Processed)
        {
            _logger.LogError(
                "ServiceRequestCancelledConsumer: gateway refund rejected for Tx {TxId}.",
                tx.Id);
            return;
        }

        // ── Create refund record ──────────────────────────────────────────────
        var refundCode = $"REF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant();
        var record = TransactionRefundRecord.Create(
            paymentTransactionId: tx.Id,
            refundCode:           refundCode,
            amount:               tx.GrossAmount,
            currencyCode:         tx.CurrencyCode,
            refundType:           RefundType.Full,
            reason:               RefundReason.ServiceRequestCancelled,
            adminNote:            $"SR {message.ServiceRequestId} auto-refund.");

        record.MarkProcessed(
            gatewayRefundReference: gatewayResult.GatewayRefundReference,
            adminNote:              null);

        await _transactions.AddRefundRecordAsync(record, ct);

        // ── Apply to parent transaction ───────────────────────────────────────
        tx.ApplyRefund(record);
        _transactions.Update(tx);

        // Consumers are NOT wrapped by AizenCommandHandlerDecorator — must call SaveChanges directly.
        await _transactions.SaveChangesAsync(ct);

        // ── Publish event ─────────────────────────────────────────────────────
        _ = _publisher.PublishAsync(new PaymentRefundedMessage
        {
            TransactionId   = tx.Id,
            TransactionCode = tx.TransactionCode,
            ContextType     = tx.ContextType,
            ContextId       = tx.ContextId,
            ContextSubId    = tx.ContextSubId,
            PayerProfileId  = tx.PayerProfileId,
            RefundedAmount  = tx.GrossAmount,
            OriginalAmount  = tx.GrossAmount,
            CurrencyCode    = tx.CurrencyCode,
            IsPartial       = false,
            Reason          = RefundReason.ServiceRequestCancelled.ToString(),
            RefundedAtUtc   = DateTime.UtcNow,
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception,
                    "Failed to publish PaymentRefundedMessage for Tx {TxId}", tx.Id);
        }, TaskContinuationOptions.OnlyOnFaulted);

        _logger.LogInformation(
            "Full refund applied via SR cancellation. TxId={TxId} Amount={Amount}",
            tx.Id, tx.GrossAmount);
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
