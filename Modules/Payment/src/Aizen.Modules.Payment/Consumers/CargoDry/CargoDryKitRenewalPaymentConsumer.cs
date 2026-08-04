using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Consumers.CargoDry;

/// <summary>
/// Listens for CargoDryKitRenewedMessage from the CargoDry module.
///
/// Responsibility:
///   When a kit is renewed and has a non-zero payment amount, creates a
///   PaymentTransactionEntity (CargoDryRenewal type) to record the platform-collected payment.
///
/// Free renewals (RenewalAmountTRY == 0) are skipped gracefully — no transaction is created.
///
/// Idempotency: uses a deterministic IdempotencyKey (KitCode + renewal date) to prevent
/// duplicate transactions if the message is delivered more than once.
///
/// Note: RecipientProfileId is null — CargoDry renewal payments go to the platform, not a provider.
/// Commission does not apply to platform-collected renewals.
/// </summary>
public sealed class CargoDryKitRenewalPaymentConsumer
    : AizenBaseMessageConsumer<CargoDryKitRenewedMessage>
{
    private readonly IPaymentTransactionRepository              _transactions;
    private readonly CommissionCalculationService               _commission;
    private readonly PaymentGatewayResolver                     _gatewayResolver;
    private readonly ILogger<CargoDryKitRenewalPaymentConsumer> _logger;

    public CargoDryKitRenewalPaymentConsumer(IServiceProvider sp) : base(sp)
    {
        _transactions    = sp.GetRequiredService<IPaymentTransactionRepository>();
        _commission      = sp.GetRequiredService<CommissionCalculationService>();
        _gatewayResolver = sp.GetRequiredService<PaymentGatewayResolver>();
        _logger          = sp.GetRequiredService<ILogger<CargoDryKitRenewalPaymentConsumer>>();
    }

    public override async Task<bool> ExecutePrepareMessage(
        CargoDryKitRenewedMessage message, CancellationToken ct)
    {
        // Free renewal — no payment record needed
        if (message.RenewalAmountTRY <= 0m)
        {
            _logger.LogInformation(
                "CargoDryKitRenewalPaymentConsumer: free renewal for Kit {KitCode}. Skipping.",
                message.KitCode);
            return false;
        }

        // Idempotency check — deterministic key: kit code + renewal month
        var idempotencyKey = BuildIdempotencyKey(message);
        var existing = await _transactions.GetByIdempotencyKeyAsync(idempotencyKey, ct);

        if (existing is not null)
        {
            _logger.LogInformation(
                "CargoDryKitRenewalPaymentConsumer: transaction already exists for Kit {KitCode} key={Key}. Idempotent skip.",
                message.KitCode, idempotencyKey);
            return false;
        }

        return true;
    }

    public override async Task ExecuteCommitMessage(
        CargoDryKitRenewedMessage message, CancellationToken ct)
    {
        if (message.RenewalAmountTRY <= 0m) return;

        var idempotencyKey = BuildIdempotencyKey(message);

        // Sanity check — idempotency may have been satisfied between Prepare and Commit
        var existing = await _transactions.GetByIdempotencyKeyAsync(idempotencyKey, ct);
        if (existing is not null) return;

        // Platform-collected payment — no commission (recipient = null, no provider override)
        // CommissionCalculationService will apply global default rate → net payout stays with platform
        var breakdown = await _commission.CalculateAsync(
            grossAmount:       message.RenewalAmountTRY,
            discountAmount:    0m,
            providerProfileId: null,
            providerPlanId:    null,
            categoryCode:      "CARGODRY_RENEWAL",
            ct:                ct);

        var transactionCode = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..24];
        var gateway         = _gatewayResolver.Resolve();

        var transaction = PaymentTransactionEntity.Create(
            transactionCode:        transactionCode,
            transactionType:        TransactionType.CargoDryRenewal,
            contextType:            TransactionContextType.CargoDry,
            contextId:              message.KitId,
            contextSubId:           null,
            payerProfileId:         message.OwnerUserId,
            recipientProfileId:     null,   // platform-collected
            grossAmount:            breakdown.GrossAmount,
            commissionAmount:       breakdown.CommissionAmount,
            commissionRateSnapshot: breakdown.CommissionRate,
            vatOnCommission:        breakdown.VatOnCommission,
            netPayoutAmount:        breakdown.NetPayoutAmount,
            discountAmount:         0m,
            currencyCode:           message.CurrencyCode,
            gatewayProvider:        gateway.ProviderKey,
            idempotencyKey:         idempotencyKey,
            escrowRequired:         false);

        // Mark as captured immediately — renewal was already paid before message was published
        transaction.Capture(gatewayReference: $"CARGORENEWAL-{message.KitCode}");

        await _transactions.AddAsync(transaction, ct);

        // Consumers are NOT wrapped by AizenCommandHandlerDecorator — must save explicitly.
        // WS1: the renewal charge is recorded by an internal Capture (no external gateway money movement),
        // and the transactions.IdempotencyKey unique index (RENEWAL-{KitCode}-{yyyyMM}) is the duplicate-proof
        // natural key. If a concurrent commit copy won the insert, swallow the unique-violation as benign.
        try
        {
            await _transactions.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (PaymentIdempotency.IsUniqueViolation(ex))
        {
            _logger.LogWarning(
                "CargoDryKitRenewalPaymentConsumer: concurrent commit already recorded the renewal charge for " +
                "Kit {KitCode} key={Key} (unique-violation swallowed). Idempotent skip.",
                message.KitCode, idempotencyKey);
            return;
        }

        _logger.LogInformation(
            "CargoDry renewal payment recorded. KitId={KitId} KitCode={KitCode} Amount={Amount} TxCode={Code}",
            message.KitId, message.KitCode, message.RenewalAmountTRY, transactionCode);
    }

    public override Task ExecuteRollbackMessage(
        CargoDryKitRenewedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogError(
            "CargoDryKitRenewalPaymentConsumer rollback for Kit {KitCode}: {Error}",
            message.KitCode, ex.Message);
        return Task.CompletedTask;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string BuildIdempotencyKey(CargoDryKitRenewedMessage message)
        => $"RENEWAL-{message.KitCode}-{message.NewExpiresAt:yyyyMM}";
}
