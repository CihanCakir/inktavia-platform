using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.CargoDry.Application.Jobs;

/// <summary>
/// Runs every day at 09:00 UTC. Publishes expiry reminder messages for kits expiring in
/// exactly 30, 7, or 1 day(s) so that the Notification module can alert the owner.
/// </summary>
public sealed class KitExpiryReminderJob : AizenRecurringJob
{
    private static readonly int[] ReminderDays = [30, 7, 1];

    public KitExpiryReminderJob(
        IAizenSchedulerLogger logger,
        IServiceProvider      serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "0 9 * * *"; // 09:00 UTC every day

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = ServiceProvider.CreateScope();
        var kits        = scope.ServiceProvider.GetRequiredService<ICargoDryKitRepository>();
        var publisher   = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();

        foreach (var days in ReminderDays)
        {
            var expiring = await kits.GetExpiringAsync(days, ct: cancellationToken);
            var targets  = expiring.Where(k => k.DaysUntilExpiry == days).ToList();

            Logger.WriteConsole($"KitExpiryReminderJob: {targets.Count} kits expiring in {days} day(s)");

            foreach (var kit in targets)
            {
                await publisher.PublishAsync(new CargoDryKitExpiringMessage
                {
                    KitId       = kit.Id,
                    KitCode     = kit.KitCode,
                    ProductName = kit.ProductCode,
                    OwnerUserId = kit.OwnerUserId!.Value,
                    VesselId    = kit.VesselId!.Value,
                    ExpiresAt   = kit.ExpiresAt!.Value,
                    DaysLeft    = days,
                }, cancellationToken);
            }
        }
    }
}
