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
using Microsoft.EntityFrameworkCore;
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
    private readonly RefundAllocationService                  _allocationService;
    private readonly IAizenMessagePublisher                   _publisher;
    private readonly ILogger<ServiceRequestCancelledConsumer> _logger;

    public ServiceRequestCancelledConsumer(IServiceProvider sp) : base(sp)
    {
        _transactions      = sp.GetRequiredService<IPaymentTransactionRepository>();
        _gatewayResolver   = sp.GetRequiredService<PaymentGatewayResolver>();
        _allocationService = sp.GetRequiredService<RefundAllocationService>();
        _publisher         = sp.GetRequiredService<IAizenMessagePublisher>();
        _logger            = sp.GetRequiredService<ILogger<ServiceRequestCancelledConsumer>>();
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

        // ── WS1 PART C: commit-level idempotency (mirror of the partial-unique DB index) ──────
        // A non-Failed full refund already claiming this transaction means a prior (or concurrent) commit
        // copy owns the refund — skip so we never issue a second gateway refund.
        if (await _transactions.FullRefundExistsAsync(tx.Id, ct))
        {
            _logger.LogInformation(
                "ServiceRequestCancelledConsumer: a full refund already exists for Tx {TxId}. " +
                "Commit idempotent skip.", tx.Id);
            return;
        }

        _logger.LogInformation(
            "ServiceRequestCancelledConsumer: triggering full refund for SR {SRId} → Tx {TxId} Amount {Amount}",
            message.ServiceRequestId, tx.Id, tx.GrossAmount);

        // ── WS1 PART B: persist the refund marker BEFORE the gateway call (fix the Tier-2 TOCTOU) ──
        // Insert the refund record in Pending state first. Its partial-unique index on (PaymentTransactionId,
        // RefundType=Full) means a concurrent second commit copy loses the insert race and returns here
        // WITHOUT calling the gateway — so the external refund happens at most once.
        var refundCode = $"REF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant();
        var record = TransactionRefundRecord.Create(
            paymentTransactionId: tx.Id,
            refundCode:           refundCode,
            amount:               tx.GrossAmount,
            currencyCode:         tx.CurrencyCode,
            refundType:           RefundType.Full,
            reason:               RefundReason.ServiceRequestCancelled,
            adminNote:            $"SR {message.ServiceRequestId} auto-refund.");   // status = Pending (marker)

        await _transactions.AddRefundRecordAsync(record, ct);
        try
        {
            await _transactions.SaveChangesAsync(ct);   // claims the natural key
        }
        catch (DbUpdateException ex) when (PaymentIdempotency.IsUniqueViolation(ex))
        {
            _logger.LogWarning(
                "ServiceRequestCancelledConsumer: concurrent commit already claimed the refund for Tx {TxId} " +
                "(unique-violation swallowed). Skipping gateway refund — idempotent.", tx.Id);
            return;
        }

        // ── Gateway call — only the marker-holder reaches here ────────────────
        var gateway = _gatewayResolver.Resolve();
        var gatewayResult = await gateway.RefundAsync(new RefundInput
        {
            TransactionId            = tx.Id,
            GatewayReference         = tx.GatewayReference ?? string.Empty,
            GatewayItemTransactionId = tx.GatewayItemTransactionId,   // BE-P9-fix §8
            RefundAmount             = tx.GrossAmount,
            Currency                 = tx.CurrencyCode,
            Reason                   = RefundReason.ServiceRequestCancelled,
            AdminNote        = $"SR {message.ServiceRequestId} cancelled on {message.CancelledAtUtc:u}. Auto-refund.",
        }, ct);

        if (!gatewayResult.Processed)
        {
            // Mark the marker Failed → excluded from the partial-unique index so a later message can retry.
            record.MarkFailed("Gateway refund rejected.");
            await _transactions.SaveChangesAsync(ct);
            _logger.LogError(
                "ServiceRequestCancelledConsumer: gateway refund rejected for Tx {TxId}. " +
                "Refund marker marked Failed; will retry on next message.", tx.Id);
            return;
        }

        // ── Gateway succeeded → finalize the marker + apply to parent transaction ──
        record.MarkProcessed(
            gatewayRefundReference: gatewayResult.GatewayRefundReference,
            adminNote:              null);

        tx.ApplyRefund(record);
        _transactions.Update(tx);

        // ── BE-P10: snapshot-driven allocation. SR cancellation on a Captured (escrow, pre-release) transaction is a
        // release-before refund (§7.2) → cancel provider net + commission, no settlement. Cause = CustomerCancelledBeforeWork.
        await _allocationService.ApplyAsync(
            tx, record,
            requestedRefundAmount: tx.GrossAmount,
            cause:                 RefundCause.CustomerCancelledBeforeWork,
            restoreBenefit:        true,
            ct:                    ct);

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
