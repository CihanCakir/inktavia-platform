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
/// Runs every 30 minutes. Finds PendingIntent transactions that have not been captured
/// within the configured timeout window and marks them as Failed.
///
/// IMPORTANT: This job does NOT stop the related ServiceRequest case immediately.
/// It publishes <see cref="PaymentFailedMessage"/> with IsRetryable = true so the
/// ServiceRequest module can transition the SR to AwaitingPayment, allowing the payer
/// to retry. Only after the admin-configured grace period should the SR be cancelled.
///
/// Configuration:
///   Payment:EscrowTimeoutHours  (default 2)
///   Payment:EscrowTimeoutBatch  (default 200)
/// </summary>
public sealed class PaymentEscrowTimeoutJob : AizenRecurringJob
{
    public PaymentEscrowTimeoutJob(
        IAizenSchedulerLogger logger,
        IServiceProvider      serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "*/30 * * * *"; // every 30 minutes

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope    = ServiceProvider.CreateScope();
        var config         = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var transactions   = scope.ServiceProvider.GetRequiredService<IPaymentTransactionRepository>();
        var publisher      = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();

        var timeoutHours   = config.GetValue<int>("Payment:EscrowTimeoutHours", 2);
        var batchSize      = config.GetValue<int>("Payment:EscrowTimeoutBatch", 200);
        var timeout        = TimeSpan.FromHours(timeoutHours);

        var stale = await transactions.GetPendingIntentOlderThanAsync(timeout, batchSize, cancellationToken);

        Logger.WriteConsole(
            $"PaymentEscrowTimeoutJob: found {stale.Count} timed-out PendingIntent transactions (>{timeoutHours}h)");

        if (stale.Count == 0) return;

        foreach (var tx in stale)
        {
            tx.MarkFailed();

            // Publish non-blocking failure message — SR module decides what to do next.
            // IsRetryable = true so payer can re-initiate payment without losing the SR.
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
                FailureReason   = "timeout",
                IsRetryable     = true,
                FailedAtUtc     = DateTime.UtcNow,
            }, cancellationToken)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Logger.WriteConsole($"PaymentEscrowTimeoutJob: failed to publish PaymentFailedMessage for Tx {tx.Id}: {t.Exception?.Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        transactions.UpdateRange(stale);
        await transactions.SaveChangesAsync(cancellationToken);

        Logger.WriteConsole($"PaymentEscrowTimeoutJob: marked {stale.Count} transactions as Failed.");
    }
}
