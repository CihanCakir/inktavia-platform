using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Jobs;

/// <summary>
/// Runs daily at 09:00 UTC. Finds Active subscriptions whose SubscriptionPeriodEnd has
/// already passed and transitions them:
///
///   AutoRenew = true  → PastDue  (awaiting manual payment in grace period)
///   AutoRenew = false → Expired  (subscription ended)
///
/// Publishes SubscriptionPaymentFailedMessage for each — Notification module sends
/// expiry/renewal reminders to the user.
///
/// ── Yol B (MVP) ───────────────────────────────────────────────────────────────
///  Auto-charging a saved card for renewal is deferred to Phase 2C.
///  In Phase 2C, Provider renewals via Iyzico and Participant renewals via Apple/Google
///  will be triggered directly from this job using IAP gateway clients.
///
///  For now, the job simply marks the status and delegates re-payment to the user:
///  Notification module → email/push → user pays manually → new subscription record created.
///
/// ── Batch safety ──────────────────────────────────────────────────────────────
///  Processes at most Payment:SubscriptionRenewalBatchSize (default 200) records per run,
///  split evenly between Provider and Participant subscriptions.
///
/// Configuration:
///   Payment:SubscriptionRenewalBatchSize  (default 200)
/// </summary>
public sealed class SubscriptionRenewalJob : AizenRecurringJob
{
    public SubscriptionRenewalJob(
        IAizenSchedulerLogger logger,
        IServiceProvider      serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "0 9 * * *"; // daily 09:00 UTC

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope           = ServiceProvider.CreateScope();
        var config                = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var providerPlanRepo      = scope.ServiceProvider.GetRequiredService<IProviderPlanRepository>();
        var participantPlanRepo   = scope.ServiceProvider.GetRequiredService<IParticipantPlanRepository>();
        var publisher             = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();
        var logger                = scope.ServiceProvider.GetRequiredService<ILogger<SubscriptionRenewalJob>>();

        var batchSize        = config.GetValue<int>("Payment:SubscriptionRenewalBatchSize", 200);
        var halfBatch        = batchSize / 2;

        // ── Provider subscriptions ────────────────────────────────────────────
        var expiredProviders = await providerPlanRepo.GetExpiredActiveSubscriptionsAsync(halfBatch, cancellationToken);

        Logger.WriteConsole($"SubscriptionRenewalJob: found {expiredProviders.Count} expired active Provider subscriptions.");

        var providerSuccessCount = 0;
        foreach (var sub in expiredProviders)
        {
            try
            {
                string newStatus;
                string reason;

                if (sub.AutoRenew)
                {
                    sub.MarkPastDue();
                    newStatus = "PastDue";
                    reason    = "auto_renewal_past_due";
                }
                else
                {
                    sub.MarkExpired();
                    newStatus = "Expired";
                    reason    = "subscription_expired";
                }

                providerPlanRepo.UpdateSubscription(sub);

                _ = publisher.PublishAsync(new SubscriptionPaymentFailedMessage
                {
                    ProfileId       = sub.ProviderProfileId,
                    ProfileType     = "Provider",
                    PlanId          = sub.ProviderPlanId,
                    PlanCode        = string.Empty,  // Enrichment not needed for notification routing
                    PlanName        = string.Empty,
                    GatewayProvider = "Iyzico",
                    FailureReason   = reason,
                    IsRenewalFailure = true,
                    NewStatus       = newStatus,
                    PeriodEndedAt   = sub.SubscriptionPeriodEnd,
                    FailedAtUtc     = DateTime.UtcNow,
                }, cancellationToken).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        logger.LogError(t.Exception,
                            "SubscriptionRenewalJob: failed to publish SubscriptionPaymentFailedMessage " +
                            "for ProviderSubscriptionId={SubId}", sub.Id);
                }, TaskContinuationOptions.OnlyOnFaulted);

                providerSuccessCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "SubscriptionRenewalJob: error processing ProviderSubscriptionId={SubId}", sub.Id);
            }
        }

        if (expiredProviders.Count > 0)
            await providerPlanRepo.SaveChangesAsync(cancellationToken);

        // ── Participant subscriptions ─────────────────────────────────────────
        var expiredParticipants = await participantPlanRepo.GetExpiredActiveSubscriptionsAsync(halfBatch, cancellationToken);

        Logger.WriteConsole($"SubscriptionRenewalJob: found {expiredParticipants.Count} expired active Participant subscriptions.");

        // TODO Phase 2C: For Participant AutoRenew, check GatewayProvider:
        //   - If Iyzico → charge saved card via Iyzico recurring
        //   - If AppleAppStore → validate/renew via IAppleAppStoreGatewayClient
        //   - If GooglePlayStore → validate/renew via IGooglePlayGatewayClient
        // Currently all Participant AutoRenew subscriptions are marked PastDue (Yol B).

        var participantSuccessCount = 0;
        foreach (var sub in expiredParticipants)
        {
            try
            {
                string newStatus;
                string reason;

                if (sub.AutoRenew)
                {
                    sub.MarkPastDue();
                    newStatus = "PastDue";
                    reason    = "auto_renewal_past_due";
                }
                else
                {
                    sub.MarkExpired();
                    newStatus = "Expired";
                    reason    = "subscription_expired";
                }

                participantPlanRepo.UpdateSubscription(sub);

                _ = publisher.PublishAsync(new SubscriptionPaymentFailedMessage
                {
                    ProfileId       = sub.ParticipantProfileId,
                    ProfileType     = "Participant",
                    PlanId          = sub.ParticipantPlanId,
                    PlanCode        = string.Empty,
                    PlanName        = string.Empty,
                    GatewayProvider = "Iyzico",  // Phase 2C: will be enriched per IAP gateway
                    FailureReason   = reason,
                    IsRenewalFailure = true,
                    NewStatus       = newStatus,
                    PeriodEndedAt   = sub.SubscriptionPeriodEnd,
                    FailedAtUtc     = DateTime.UtcNow,
                }, cancellationToken).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        logger.LogError(t.Exception,
                            "SubscriptionRenewalJob: failed to publish SubscriptionPaymentFailedMessage " +
                            "for ParticipantSubscriptionId={SubId}", sub.Id);
                }, TaskContinuationOptions.OnlyOnFaulted);

                participantSuccessCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "SubscriptionRenewalJob: error processing ParticipantSubscriptionId={SubId}", sub.Id);
            }
        }

        if (expiredParticipants.Count > 0)
            await participantPlanRepo.SaveChangesAsync(cancellationToken);

        Logger.WriteConsole(
            $"SubscriptionRenewalJob complete: " +
            $"providers processed={providerSuccessCount}/{expiredProviders.Count}, " +
            $"participants processed={participantSuccessCount}/{expiredParticipants.Count}.");
    }
}
