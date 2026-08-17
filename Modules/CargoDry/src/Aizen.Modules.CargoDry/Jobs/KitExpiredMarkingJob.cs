using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.CargoDry.Application.Jobs;

/// <summary>
/// Runs every hour. Finds activated kits whose ExpiresAt has passed and marks them as Expired,
/// writes an activation log entry, and publishes a CargoDryKitExpiredMessage for notifications.
/// </summary>
public sealed class KitExpiredMarkingJob : AizenRecurringJob
{
    public KitExpiredMarkingJob(
        IAizenSchedulerLogger logger,
        IServiceProvider      serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "0 * * * *"; // every hour at :00

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope    = ServiceProvider.CreateScope();
        var kits           = scope.ServiceProvider.GetRequiredService<ICargoDryKitRepository>();
        var publisher      = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();
        var activationLogs = scope.ServiceProvider.GetRequiredService<ICargoDryActivationLogRepository>();
        var cache          = scope.ServiceProvider.GetRequiredService<IAizenDistributedCache>();

        var expired = await kits.GetExpiredUnmarkedAsync(ct: cancellationToken);
        Logger.WriteConsole($"KitExpiredMarkingJob: marking {expired.Count} kits as expired");

        foreach (var kit in expired)
        {
            kit.MarkExpired();

            var logDoc = new CargoDryActivationLogDocument
            {
                KitId        = kit.Id,
                SerialNumber = kit.SerialNumber,
                KitCode      = kit.KitCode,
                ProductCode  = kit.ProductCode,
                BatchCode    = kit.BatchCode,
                EventType    = "Expired",
                OwnerUserId  = kit.OwnerUserId,
                VesselId     = kit.VesselId,
                OccurredAt   = DateTimeOffset.UtcNow,
                DateKey      = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            };

            _ = activationLogs.InsertAsync(logDoc, cancellationToken)
                .ContinueWith(
                    t => Logger.WriteConsole($"Failed to write expiry log for Kit {kit.Id}: {t.Exception?.Message}"),
                    TaskContinuationOptions.OnlyOnFaulted);

            await publisher.PublishAsync(new CargoDryKitExpiredMessage
            {
                KitId       = kit.Id,
                KitCode     = kit.KitCode,
                OwnerUserId = kit.OwnerUserId!.Value,
                VesselId    = kit.VesselId!.Value,
            }, cancellationToken);
        }

        await kits.SaveChangesAsync(cancellationToken);

        if (expired.Count > 0)
            await cache.RemoveAsync<CargoDryStatsDto>("cargodry:stats:global", cancellationToken);
    }
}
