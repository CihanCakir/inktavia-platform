using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Payout;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Consumers.ServiceRequest;

/// <summary>
/// Listens for ServiceRequestCompletedMessage from the ServiceRequest module.
/// When SR completion is approved, releases escrowed funds to the provider via gateway,
/// creates a PayoutRecord, and publishes PaymentEscrowReleasedMessage.
///
/// Idempotency: if the transaction is already Released, the consumer skips gracefully.
/// </summary>
public sealed class ServiceRequestCompletedConsumer
    : AizenBaseMessageConsumer<ServiceRequestCompletedMessage>
{
    private readonly IPaymentTransactionRepository             _transactions;
    private readonly IPayoutRecordRepository                   _payouts;
    private readonly PaymentGatewayResolver                    _gatewayResolver;
    private readonly IAizenMessagePublisher                    _publisher;
    private readonly ILogger<ServiceRequestCompletedConsumer>  _logger;

    public ServiceRequestCompletedConsumer(IServiceProvider sp) : base(sp)
    {
        _transactions    = sp.GetRequiredService<IPaymentTransactionRepository>();
        _payouts         = sp.GetRequiredService<IPayoutRecordRepository>();
        _gatewayResolver = sp.GetRequiredService<PaymentGatewayResolver>();
        _publisher       = sp.GetRequiredService<IAizenMessagePublisher>();
        _logger          = sp.GetRequiredService<ILogger<ServiceRequestCompletedConsumer>>();
    }

    public override async Task<bool> ExecutePrepareMessage(
        ServiceRequestCompletedMessage message, CancellationToken ct)
    {
        var tx = await _transactions.GetByContextAsync(
            TransactionContextType.ServiceRequest, message.ServiceRequestId, ct);

        if (tx is null)
        {
            _logger.LogWarning(
                "ServiceRequestCompletedConsumer: no transaction for SR {SRId}. Skipping escrow release.",
                message.ServiceRequestId);
            return false;
        }

        if (tx.Status == PaymentTransactionStatus.Released)
        {
            _logger.LogInformation(
                "ServiceRequestCompletedConsumer: Tx {TxId} already Released. Idempotent skip.",
                tx.Id);
            return false;
        }

        return true;
    }

    public override async Task ExecuteCommitMessage(
        ServiceRequestCompletedMessage message, CancellationToken ct)
    {
        var tx = await _transactions.GetByContextAsync(
            TransactionContextType.ServiceRequest, message.ServiceRequestId, ct);

        if (tx is null || tx.Status == PaymentTransactionStatus.Released) return;

        // ── WS1 PART C: commit-level idempotency (mirror of the partial-unique DB index) ──────
        // A non-Failed payout already claiming this transaction means a prior (or concurrent) commit copy
        // owns the release — skip so we never issue a second gateway payout.
        if (await _payouts.ActivePayoutExistsAsync(tx.Id, ct))
        {
            _logger.LogInformation(
                "ServiceRequestCompletedConsumer: an active payout already exists for Tx {TxId}. " +
                "Commit idempotent skip.", tx.Id);
            return;
        }

        _logger.LogInformation(
            "ServiceRequestCompletedConsumer: releasing escrow for SR {SRId} → Tx {TxId}",
            message.ServiceRequestId, tx.Id);

        // ── WS1 PART B: persist the payout marker BEFORE the gateway call (fix the Tier-2 TOCTOU) ──
        // Insert the PayoutRecord in Pending state first. Its partial-unique index on PaymentTransactionId
        // means a concurrent second commit copy loses the insert race and returns here WITHOUT calling the
        // gateway — so the external escrow release happens at most once, no matter how many commit copies race.
        var gateway = _gatewayResolver.Resolve();
        var payout = PayoutRecordEntity.Create(
            providerProfileId:    tx.RecipientProfileId ?? 0,
            paymentTransactionId: tx.Id,
            amount:               tx.NetPayoutAmount,
            currencyCode:         tx.CurrencyCode,
            gatewayProvider:      gateway.ProviderKey);   // status = Pending (marker)

        await _payouts.AddAsync(payout, ct);
        try
        {
            await _payouts.SaveChangesAsync(ct);   // claims the natural key
        }
        catch (DbUpdateException ex) when (PaymentIdempotency.IsUniqueViolation(ex))
        {
            _logger.LogWarning(
                "ServiceRequestCompletedConsumer: concurrent commit already claimed the payout for Tx {TxId} " +
                "(unique-violation swallowed). Skipping gateway release — idempotent.", tx.Id);
            return;
        }

        // ── Gateway call — only the marker-holder reaches here ────────────────
        var payoutResult = await gateway.ReleaseEscrowAsync(new ReleaseEscrowInput
        {
            TransactionId            = tx.Id,
            GatewayReference         = tx.GatewayReference ?? string.Empty,
            GatewayItemTransactionId = tx.GatewayItemTransactionId,   // BE-P9-fix §4
            ProviderNetAmount        = tx.NetPayoutAmount,
            AdminNote                = message.AdminNote ?? $"SR {message.ServiceRequestId} completed. Auto-release.",
        }, ct);

        if (!payoutResult.Processed)
        {
            // Mark the marker Failed → excluded from the partial-unique index so a later message can retry.
            payout.MarkFailed("Gateway escrow release rejected.");
            await _payouts.SaveChangesAsync(ct);
            _logger.LogError(
                "ServiceRequestCompletedConsumer: gateway release failed for Tx {TxId}. " +
                "Payout marker marked Failed; will retry on next message.", tx.Id);
            return;
        }

        // ── Gateway succeeded → finalize the marker + release the transaction ──
        payout.MarkCompleted(payoutResult.GatewayPayoutId, message.AdminNote);
        tx.Release();
        _transactions.Update(tx);

        // Consumers are NOT wrapped by AizenCommandHandlerDecorator — must call SaveChanges directly.
        await _transactions.SaveChangesAsync(ct);

        // ── Publish event ─────────────────────────────────────────────────────
        _ = _publisher.PublishAsync(new PaymentEscrowReleasedMessage
        {
            TransactionId      = tx.Id,
            TransactionCode    = tx.TransactionCode,
            ContextType        = tx.ContextType,
            ContextId          = tx.ContextId,
            ContextSubId       = tx.ContextSubId,
            RecipientProfileId = tx.RecipientProfileId ?? 0,
            PayerProfileId     = tx.PayerProfileId,
            NetPayoutAmount    = tx.NetPayoutAmount,
            CommissionAmount   = tx.CommissionAmount,
            CurrencyCode       = tx.CurrencyCode,
            GatewayPayoutId    = payoutResult.GatewayPayoutId,
            ReleasedAtUtc      = DateTime.UtcNow,
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception,
                    "Failed to publish PaymentEscrowReleasedMessage for Tx {TxId}", tx.Id);
        }, TaskContinuationOptions.OnlyOnFaulted);

        _logger.LogInformation(
            "Escrow released via SR completion. TxId={TxId} PayoutId={PayoutId} Net={Net}",
            tx.Id, payout.Id, tx.NetPayoutAmount);
    }

    public override Task ExecuteRollbackMessage(
        ServiceRequestCompletedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogError(
            "ServiceRequestCompletedConsumer rollback for SR {SRId}: {Error}",
            message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
