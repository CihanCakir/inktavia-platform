using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Application.Gateway.Iyzico;
using Aizen.Modules.Payment.Application.Gateway.Iyzico.Models;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Payment.Jobs;

/// <summary>
/// Runs every 15 minutes. Finds PendingIntent transactions that are old enough to
/// have received a gateway webhook but did not — and re-queries Iyzico to resolve them.
///
/// ── Why this job exists ────────────────────────────────────────────────────
///  Gateway webhooks are not guaranteed to arrive. If Iyzico sends a payment capture
///  notification but our webhook endpoint misses it (network blip, downtime, etc.),
///  the transaction stays in PendingIntent forever until PaymentEscrowTimeoutJob kills it.
///  This job re-queries Iyzico proactively so the transaction is resolved faster.
///
/// ── Window logic ──────────────────────────────────────────────────────────
///  Only transactions older than Payment:WebhookRetryThresholdMinutes (default 15min)
///  and younger than Payment:EscrowTimeoutHours (default 2h) are checked.
///  This avoids overlapping with PaymentEscrowTimeoutJob's window.
///
/// ── Iyzico re-query ───────────────────────────────────────────────────────
///  Uses IyzicoHttpClient.RetrieveCheckoutFormAsync with the transaction's TransactionCode
///  as the conversationId. If the response indicates a captured payment, the transaction
///  is captured locally and PaymentCapturedMessage is published.
///  If the response indicates failure, the transaction is marked Failed and
///  PaymentFailedMessage(IsRetryable=true) is published.
///
/// Configuration:
///   Payment:WebhookRetryThresholdMinutes  (default 15)
///   Payment:WebhookRetryBatchSize         (default 50)
///   Payment:EscrowTimeoutHours            (default 2)
/// </summary>
public sealed class PaymentWebhookRetryJob : AizenRecurringJob
{
    public PaymentWebhookRetryJob(
        IAizenSchedulerLogger logger,
        IServiceProvider      serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "*/15 * * * *"; // every 15 minutes

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope    = ServiceProvider.CreateScope();
        var config         = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var transactions   = scope.ServiceProvider.GetRequiredService<IPaymentTransactionRepository>();
        var publisher      = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();
        var iyzico         = scope.ServiceProvider.GetRequiredService<IyzicoHttpClient>();

        var retryThresholdMinutes = config.GetValue<int>("Payment:WebhookRetryThresholdMinutes", 15);
        var timeoutHours          = config.GetValue<int>("Payment:EscrowTimeoutHours", 2);
        var batchSize             = config.GetValue<int>("Payment:WebhookRetryBatchSize", 50);

        var olderThan   = TimeSpan.FromMinutes(retryThresholdMinutes);
        var youngerThan = TimeSpan.FromHours(timeoutHours);

        var candidates = await transactions.GetPendingIntentInRangeAsync(
            olderThan, youngerThan, batchSize, cancellationToken);

        Logger.WriteConsole(
            $"PaymentWebhookRetryJob: found {candidates.Count} PendingIntent transactions " +
            $"({retryThresholdMinutes}min–{timeoutHours}h old) for Iyzico status re-query.");

        if (candidates.Count == 0) return;

        var captured = 0;
        var failed   = 0;
        var pending  = 0;

        foreach (var tx in candidates)
        {
            try
            {
                // Re-query Iyzico using the GatewayReference (= checkout form token returned
                // at initialization). Null means the checkout form was never fully initialized.
                if (string.IsNullOrWhiteSpace(tx.GatewayReference))
                {
                    Logger.WriteConsole(
                        $"PaymentWebhookRetryJob: Tx {tx.Id} has no GatewayReference — skipping Iyzico re-query.");
                    pending++;
                    continue;
                }

                var result = await iyzico.RetrieveCheckoutFormAsync(
                    new IyzicoRetrieveCheckoutRequest { Token = tx.GatewayReference },
                    cancellationToken);

                if (result is null)
                {
                    Logger.WriteConsole(
                        $"PaymentWebhookRetryJob: null response from Iyzico for Tx {tx.Id} (Code={tx.TransactionCode}). Skipping.");
                    pending++;
                    continue;
                }

                // Iyzico status "success" means the payment was captured but webhook was missed.
                if (result.Status?.Equals("success", StringComparison.OrdinalIgnoreCase) == true
                    && result.PaymentStatus?.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase) == true)
                {
                    tx.Capture(result.Token ?? tx.GatewayReference);
                    transactions.Update(tx);
                    await transactions.SaveChangesAsync(cancellationToken);

                    _ = publisher.PublishAsync(new PaymentCapturedMessage
                    {
                        TransactionId      = tx.Id,
                        TransactionCode    = tx.TransactionCode,
                        GatewayReference   = tx.GatewayReference ?? string.Empty,
                        ContextType        = tx.ContextType,
                        ContextId          = tx.ContextId,
                        ContextSubId       = tx.ContextSubId,
                        PayerProfileId     = tx.PayerProfileId,
                        RecipientProfileId = tx.RecipientProfileId,
                        GrossAmount        = tx.GrossAmount,
                        NetPayoutAmount    = tx.NetPayoutAmount,
                        CommissionAmount   = tx.CommissionAmount,
                        CurrencyCode       = tx.CurrencyCode,
                        CapturedAtUtc      = DateTime.UtcNow,
                    }, cancellationToken);

                    Logger.WriteConsole(
                        $"PaymentWebhookRetryJob: Tx {tx.Id} recovered — webhook was missed. Captured from re-query.");
                    captured++;
                }
                else if (result.Status?.Equals("failure", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Gateway definitively failed — mark accordingly.
                    tx.MarkFailed();
                    transactions.Update(tx);
                    await transactions.SaveChangesAsync(cancellationToken);

                    _ = publisher.PublishAsync(new PaymentFailedMessage
                    {
                        TransactionId  = tx.Id,
                        TransactionCode = tx.TransactionCode,
                        ContextType    = tx.ContextType,
                        ContextId      = tx.ContextId,
                        ContextSubId   = tx.ContextSubId,
                        PayerProfileId = tx.PayerProfileId,
                        GrossAmount    = tx.GrossAmount,
                        CurrencyCode   = tx.CurrencyCode,
                        FailureReason  = "gateway_failure_confirmed_on_retry",
                        IsRetryable    = false,
                        FailedAtUtc    = DateTime.UtcNow,
                    }, cancellationToken);

                    Logger.WriteConsole(
                        $"PaymentWebhookRetryJob: Tx {tx.Id} confirmed failed by Iyzico re-query.");
                    failed++;
                }
                else
                {
                    // Still pending at gateway — nothing to do yet.
                    pending++;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(
                    $"PaymentWebhookRetryJob: error re-querying Iyzico for Tx {tx.Id}: {ex.Message}");
            }
        }

        Logger.WriteConsole(
            $"PaymentWebhookRetryJob complete: recovered={captured}, confirmed-failed={failed}, still-pending={pending}");
    }
}
