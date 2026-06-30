using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Jobs;

/// <summary>
/// Runs every hour. Finds Captured ServiceRequest transactions whose escrow was never
/// released (i.e. ServiceRequestCompletedMessage was never received by the Payment module)
/// after the configured threshold, and re-publishes ServiceRequestCompletedMessage to
/// trigger ServiceRequestCompletedConsumer and release the stuck escrow.
///
/// ── Why this is safe ─────────────────────────────────────────────────────────
///  ServiceRequestCompletedConsumer is idempotent: if the tx is already Released,
///  ExecutePrepareMessage returns false and no duplicate release is attempted.
///  The job only acts on Captured (not Released) transactions, so the window for
///  double-processing is zero in the happy path.
///
/// ── Cross-module note ─────────────────────────────────────────────────────────
///  The job does NOT query the SR module. It detects stuck escrow purely from the
///  payment side: Captured + ContextType == ServiceRequest + age > threshold.
///  This is safe because a provider who completed a job has the SR module mark it
///  complete; if that event was dropped, this job becomes the safety net.
///
/// ── AdminNote ─────────────────────────────────────────────────────────────────
///  The PayoutRecord created by ServiceRequestCompletedConsumer will contain an
///  AdminNote indicating the release was triggered automatically by this job.
///
/// Configuration:
///   Payment:AutoReleaseHours      (default 72)   — threshold for stuck escrow detection
///   Payment:AutoReleaseBatchSize  (default 100)  — max transactions per run
/// </summary>
public sealed class PaymentAutoReleaseEligibilityJob : AizenRecurringJob
{
    public PaymentAutoReleaseEligibilityJob(
        IAizenSchedulerLogger logger,
        IServiceProvider      serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "0 * * * *"; // every hour

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope   = ServiceProvider.CreateScope();
        var config        = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var txRepo        = scope.ServiceProvider.GetRequiredService<IPaymentTransactionRepository>();
        var publisher     = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();
        var logger        = scope.ServiceProvider.GetRequiredService<ILogger<PaymentAutoReleaseEligibilityJob>>();

        var autoReleaseHours = config.GetValue<int>("Payment:AutoReleaseHours", 72);
        var batchSize        = config.GetValue<int>("Payment:AutoReleaseBatchSize", 100);

        var stuckTransactions = await txRepo.GetCapturedOlderThanAsync(
            TransactionContextType.ServiceRequest,
            TimeSpan.FromHours(autoReleaseHours),
            batchSize,
            cancellationToken);

        Logger.WriteConsole(
            $"PaymentAutoReleaseEligibilityJob: found {stuckTransactions.Count} Captured SR transactions " +
            $"older than {autoReleaseHours}h.");

        if (stuckTransactions.Count == 0) return;

        var autoReleaseNote = $"Auto-released by PaymentAutoReleaseEligibilityJob after {autoReleaseHours}h " +
                              $"with no SR completion signal.";

        var successCount = 0;
        var failCount    = 0;

        foreach (var tx in stuckTransactions)
        {
            try
            {
                await publisher.PublishAsync(new ServiceRequestCompletedMessage
                {
                    ServiceRequestId  = tx.ContextId,
                    OfferId           = tx.ContextSubId ?? 0,
                    ProviderProfileId = tx.RecipientProfileId ?? 0,
                    PayerProfileId    = tx.PayerProfileId,
                    CompletedAtUtc    = DateTime.UtcNow,
                    AdminNote         = autoReleaseNote,
                }, cancellationToken);

                successCount++;

                logger.LogInformation(
                    "PaymentAutoReleaseEligibilityJob: published auto-release for SR {SRId} " +
                    "TxId={TxId} Age={AgeH:F1}h",
                    tx.ContextId, tx.Id,
                    (DateTime.UtcNow - tx.CreateDate)?.TotalHours ?? 0);
            }
            catch (Exception ex)
            {
                failCount++;
                logger.LogError(ex,
                    "PaymentAutoReleaseEligibilityJob: failed to publish auto-release for " +
                    "SR {SRId} TxId={TxId}",
                    tx.ContextId, tx.Id);
            }
        }

        Logger.WriteConsole(
            $"PaymentAutoReleaseEligibilityJob: published {successCount} auto-release message(s), " +
            $"{failCount} failed.");
    }
}
