using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Payment.Jobs;

/// <summary>
/// Runs every hour at :15. Finds ServiceRequest-type PendingIntent transactions
/// that fall in configured reminder windows (e.g., pending for exactly 1h or 24h)
/// and publishes <see cref="PaymentReminderRequestedMessage"/> so the Notification
/// module can alert the payer.
///
/// Reminder windows check: the job checks the age of each pending transaction and
/// publishes a reminder if the age falls in a ±15 minute band around each configured
/// reminder threshold (to avoid missing the window between cron runs).
///
/// Configuration:
///   Payment:ReminderWindowsHours  (default [1, 24])   — comma-separated or JSON array
///   Payment:EscrowTimeoutHours    (default 2)         — used to skip timeouts that will be handled by timeout job
///   Payment:ReminderBatch         (default 500)
/// </summary>
public sealed class PaymentReminderJob : AizenRecurringJob
{
    private static readonly int[] DefaultReminderWindows = [1, 24];

    public PaymentReminderJob(
        IAizenSchedulerLogger logger,
        IServiceProvider      serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "15 * * * *"; // every hour at :15

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope   = ServiceProvider.CreateScope();
        var config        = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var transactions  = scope.ServiceProvider.GetRequiredService<IPaymentTransactionRepository>();
        var publisher     = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();

        var timeoutHours  = config.GetValue<int>("Payment:EscrowTimeoutHours", 2);
        var batchSize     = config.GetValue<int>("Payment:ReminderBatch", 500);

        // Parse configured reminder windows (hours)
        var windowsRaw    = config.GetSection("Payment:ReminderWindowsHours").Get<int[]>();
        var windows       = windowsRaw?.Length > 0 ? windowsRaw : DefaultReminderWindows;

        // Fetch all PendingIntent transactions older than 30 minutes (earliest reminder window minus buffer)
        var allPending = await transactions.GetPendingIntentOlderThanAsync(
            TimeSpan.FromMinutes(30), batchSize, cancellationToken);

        // Only SR-type escrow transactions need reminders
        var srPending = allPending
            .Where(tx => tx.ContextType == TransactionContextType.ServiceRequest
                      && tx.EscrowRequired)
            .ToList();

        Logger.WriteConsole($"PaymentReminderJob: evaluating {srPending.Count} pending SR transactions.");

        var now       = DateTime.UtcNow;
        var band      = TimeSpan.FromMinutes(15); // ±15 min window around each threshold
        var published = 0;

        foreach (var tx in srPending)
        {
            var age = now - tx.CreateDate;

            // Skip transactions that have already hit the timeout (handled by EscrowTimeoutJob)
            if (age >= TimeSpan.FromHours(timeoutHours)) continue;

            foreach (var windowHours in windows)
            {
                var windowAge = TimeSpan.FromHours(windowHours);
                if (age >= windowAge - band && age < windowAge + band)
                {
                    _ = publisher.PublishAsync(new PaymentReminderRequestedMessage
                    {
                        TransactionId    = tx.Id,
                        TransactionCode  = tx.TransactionCode,
                        ContextType      = tx.ContextType,
                        ContextId        = tx.ContextId,
                        PayerProfileId   = tx.PayerProfileId,
                        GrossAmount      = tx.GrossAmount,
                        CurrencyCode     = tx.CurrencyCode,
                        ReminderWindow   = $"{windowHours}h",
                        PendingSinceUtc  = tx.CreateDate ?? DateTime.UtcNow,
                    }, cancellationToken)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            Logger.WriteConsole($"PaymentReminderJob: publish failed for Tx {tx.Id}: {t.Exception?.Message}");
                    }, TaskContinuationOptions.OnlyOnFaulted);

                    published++;
                    break; // Only one reminder per transaction per run
                }
            }
        }

        Logger.WriteConsole($"PaymentReminderJob: published {published} reminder messages.");
    }
}
