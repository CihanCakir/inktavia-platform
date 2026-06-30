using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Payment.Application.Jobs;

/// <summary>
/// Runs daily at 02:00 UTC. Processes PayoutRecord entities in Pending status.
///
/// Payout lifecycle:
///   1. ReleasePaymentEscrowCommand creates a PayoutRecord (Pending) and calls Iyzico approval.
///   2. For manual gateway: PayoutRecord stays Pending until admin confirms via MarkPayoutCompleteCommand.
///   3. For Iyzico: the approval call in ReleasePaymentEscrow should already complete the payout.
///      This job acts as a safety net for any Pending records left behind (e.g., network failure).
///
/// Strategy:
///   - If gateway = "manual": skip (admin must confirm individually)
///   - If gateway = "iyzico" and record is older than Payment:PayoutAutoRetryHours: mark as Failed + alert
///   - Publishes PayoutCompletedMessage for successfully detected completed payouts
///
/// Configuration:
///   Payment:PayoutAutoRetryHours  (default 48)  — Iyzico payouts older than this → Failed
///   Payment:PayoutBatch           (default 200)
/// </summary>
public sealed class PayoutProcessingJob : AizenRecurringJob
{
    public PayoutProcessingJob(
        IAizenSchedulerLogger logger,
        IServiceProvider      serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "0 2 * * *"; // 02:00 UTC every day

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope  = ServiceProvider.CreateScope();
        var config       = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var payouts      = scope.ServiceProvider.GetRequiredService<IPayoutRecordRepository>();
        var publisher    = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();

        var retryHours   = config.GetValue<int>("Payment:PayoutAutoRetryHours", 48);
        var cutoff       = DateTime.UtcNow - TimeSpan.FromHours(retryHours);

        var pendingPayouts = await payouts.GetPendingAsync(cancellationToken);

        Logger.WriteConsole($"PayoutProcessingJob: {pendingPayouts.Count} pending payout records found.");

        var staleIyzico = pendingPayouts
            .Where(p => p.GatewayProvider == "iyzico" && p.RequestedAt <= cutoff)
            .ToList();

        Logger.WriteConsole($"PayoutProcessingJob: {staleIyzico.Count} stale Iyzico payouts (>{retryHours}h)");

        foreach (var payout in staleIyzico)
        {
            payout.MarkFailed(
                $"Iyzico payout not confirmed within {retryHours}h. Manual intervention required.");

            _ = publisher.PublishAsync(new PayoutCompletedMessage
            {
                PayoutRecordId    = payout.Id,
                TransactionId     = payout.PaymentTransactionId,
                ProviderProfileId = payout.ProviderProfileId,
                Amount            = payout.Amount,
                CurrencyCode      = payout.CurrencyCode,
                GatewayProvider   = payout.GatewayProvider,
                GatewayPayoutId   = payout.GatewayPayoutId,
                ProcessedAtUtc    = DateTime.UtcNow,
            }, cancellationToken)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Logger.WriteConsole($"PayoutProcessingJob: publish failed for Payout {payout.Id}: {t.Exception?.Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        if (staleIyzico.Count > 0)
        {
            staleIyzico.ForEach(p => payouts.Update(p));
            await payouts.SaveChangesAsync(cancellationToken);
        }

        // Log manual pending payouts as a reminder (admin action required)
        var manualPending = pendingPayouts
            .Where(p => p.GatewayProvider == "manual")
            .ToList();

        if (manualPending.Count > 0)
            Logger.WriteConsole(
                $"PayoutProcessingJob: {manualPending.Count} manual payouts awaiting admin confirmation.");
    }
}
