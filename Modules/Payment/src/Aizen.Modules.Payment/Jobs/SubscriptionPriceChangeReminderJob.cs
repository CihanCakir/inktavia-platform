using System.Globalization;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Application.Queries.GetSubscriptionsWithUpcomingPriceChange;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Payment.Jobs;

/// <summary>
/// N1 (§13.2) — the daily renewal-price-change reminder (07:00 UTC). Reuses the P4
/// <see cref="GetSubscriptionsWithUpcomingPriceChangeQuery"/> (active auto-renewing subscriptions whose resolved renewal
/// price differs from the current, within the configurable lead window, default 14 days) and, for each, publishes a
/// <see cref="SubscriptionPriceChangeUpcomingMessage"/> so the Notification module reminds the provider.
///
/// <para><b>Idempotency — one reminder per upcoming price-version, not daily spam:</b> the subscription carries a
/// <c>PriceChangeReminderVersionKey</c> marker (renewal date + upcoming amount). The job publishes only when the current
/// upcoming version differs from the stored one, then stamps it. When the change takes effect (renewal advances) or the
/// target price re-versions, the key changes → one fresh reminder. Multi-replica safe (the trigger fires once
/// cluster-wide; the per-row marker guards re-selection). Additive — reads P4, never mutates pricing.</para>
///
/// Config: <c>Payment:PriceChangeReminderLeadDays</c> (default 14).
/// </summary>
public sealed class SubscriptionPriceChangeReminderJob : AizenRecurringJob
{
    public SubscriptionPriceChangeReminderJob(IAizenSchedulerLogger logger, IServiceProvider serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "0 7 * * *"; // 07:00 UTC every day

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        int leadDays;
        using (var scope = ServiceProvider.CreateScope())
            leadDays = Math.Max(0, scope.ServiceProvider.GetRequiredService<IConfiguration>()
                .GetValue("Payment:PriceChangeReminderLeadDays", 14));

        List<UpcomingPriceChangeItem> items;
        using (var scope = ServiceProvider.CreateScope())
        {
            var cqrs = scope.ServiceProvider.GetRequiredService<IAizenCQRSProcessor>();
            items = await cqrs.ProcessAsync<List<UpcomingPriceChangeItem>>(
                new GetSubscriptionsWithUpcomingPriceChangeQuery { WithinDays = leadDays }, ct) ?? new();
        }

        Logger.WriteConsole($"SubscriptionPriceChangeReminderJob: {items.Count} subscription(s) with an upcoming price change.");

        var sent = 0;
        foreach (var item in items)
        {
            try
            {
                using var scope = ServiceProvider.CreateScope();
                var repo      = scope.ServiceProvider.GetRequiredService<IProviderPlanRepository>();
                var publisher = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();

                var sub = await repo.GetSubscriptionByIdAsync(item.SubscriptionId, ct);
                if (sub is null) continue;

                // Per-price-version key — a different renewal date OR upcoming amount re-arms exactly one reminder.
                var versionKey = $"{item.RenewalDateUtc:O}|{item.UpcomingPriceAmount.ToString(CultureInfo.InvariantCulture)}";
                if (!sub.NeedsPriceChangeReminder(versionKey))
                    continue;   // already reminded about this exact upcoming change

                var plan = await repo.GetByIdAsync(item.ProviderPlanId, ct);

                await publisher.PublishAsync(new SubscriptionPriceChangeUpcomingMessage
                {
                    SubscriptionId    = sub.Id,
                    ProviderProfileId = item.ProviderProfileId,
                    PlanCode          = plan?.PlanCode ?? string.Empty,
                    CurrentPrice      = item.CurrentPaidAmount,
                    NewPrice          = item.UpcomingPriceAmount,
                    CurrencyCode      = item.CurrencyCode,
                    EffectiveAtUtc    = item.RenewalDateUtc,
                }, ct);

                sub.MarkPriceChangeReminderSent(versionKey);
                repo.UpdateSubscription(sub);
                await repo.SaveChangesAsync(ct);
                sent++;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole($"SubscriptionPriceChangeReminderJob: subscription {item.SubscriptionId} failed: {ex.Message}");
            }
        }

        Logger.WriteConsole($"SubscriptionPriceChangeReminderJob: published {sent} reminder(s).");
    }
}
