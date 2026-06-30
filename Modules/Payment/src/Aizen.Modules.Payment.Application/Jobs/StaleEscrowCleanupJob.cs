using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Payment.Application.Jobs;

/// <summary>
/// Runs daily at 03:00 UTC. Cancels very old PendingIntent transactions that
/// have survived past both the EscrowTimeoutJob and any retry grace period.
///
/// A transaction that is still PendingIntent after Payment:StaleCleanupDays has
/// likely been abandoned by the payer. This job cancels them permanently and
/// publishes PaymentFailedMessage with IsRetryable = false so the ServiceRequest
/// module can cancel the SR.
///
/// Scenarios this job handles:
///   1. Iyzico checkout form opened but never completed (card abandoned)
///   2. Gateway webhook never received (rare Iyzico failure)
///   3. EscrowTimeoutJob failed mid-batch on a previous run
///
/// Configuration:
///   Payment:StaleCleanupDays  (default 7)
///   Payment:StaleCleanupBatch (default 500)
/// </summary>
public sealed class StaleEscrowCleanupJob : AizenRecurringJob
{
    public StaleEscrowCleanupJob(
        IAizenSchedulerLogger logger,
        IServiceProvider      serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "0 3 * * *"; // 03:00 UTC every day

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope   = ServiceProvider.CreateScope();
        var config        = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var transactions  = scope.ServiceProvider.GetRequiredService<IPaymentTransactionRepository>();
        var publisher     = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();

        var staleDays  = config.GetValue<int>("Payment:StaleCleanupDays", 7);
        var batchSize  = config.GetValue<int>("Payment:StaleCleanupBatch", 500);
        var staleAge   = TimeSpan.FromDays(staleDays);

        var stale = await transactions.GetPendingIntentOlderThanAsync(staleAge, batchSize, cancellationToken);

        Logger.WriteConsole(
            $"StaleEscrowCleanupJob: found {stale.Count} PendingIntent transactions older than {staleDays} day(s).");

        if (stale.Count == 0) return;

        foreach (var tx in stale)
        {
            // Use Cancel() for PendingIntent — permanent abandonment, not a gateway failure
            try
            {
                tx.Cancel(CancellationReason.AbandonedIntent);
            }
            catch (InvalidOperationException)
            {
                // Edge case: status may have changed between query and processing
                Logger.WriteConsole($"StaleEscrowCleanupJob: skipping Tx {tx.Id} — no longer PendingIntent.");
                continue;
            }

            // Notify other modules: IsRetryable = false → SR should be cancelled
            _ = publisher.PublishAsync(new PaymentFailedMessage
            {
                TransactionId   = tx.Id,
                TransactionCode = tx.TransactionCode,
                ContextType     = tx.ContextType,
                ContextId       = tx.ContextId,
                ContextSubId    = tx.ContextSubId,
                PayerProfileId  = tx.PayerProfileId,
                GrossAmount     = tx.GrossAmount,
                CurrencyCode    = tx.CurrencyCode,
                FailureReason   = $"stale_abandoned_{staleDays}d",
                IsRetryable     = false,  // Permanent — SR should be cancelled
                FailedAtUtc     = DateTime.UtcNow,
            }, cancellationToken)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Logger.WriteConsole($"StaleEscrowCleanupJob: publish failed for Tx {tx.Id}: {t.Exception?.Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        transactions.UpdateRange(stale.Where(t => t.Status == PaymentTransactionStatus.Cancelled));
        await transactions.SaveChangesAsync(cancellationToken);

        Logger.WriteConsole($"StaleEscrowCleanupJob: cancelled {stale.Count} stale transactions.");
    }
}
