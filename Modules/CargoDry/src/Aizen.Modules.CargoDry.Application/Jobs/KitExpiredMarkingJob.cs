using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Jobs;

public sealed class KitExpiredMarkingJob : BackgroundService
{
    private readonly IServiceScopeFactory       _scopeFactory;
    private readonly ILogger<KitExpiredMarkingJob> _logger;

    public KitExpiredMarkingJob(IServiceScopeFactory sf, ILogger<KitExpiredMarkingJob> logger)
    {
        _scopeFactory = sf;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "KitExpiredMarkingJob failed"); }
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope    = _scopeFactory.CreateScope();
        var kits           = scope.ServiceProvider.GetRequiredService<ICargoDryKitRepository>();
        var publisher      = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();
        var activationLogs = scope.ServiceProvider.GetRequiredService<ICargoDryActivationLogRepository>();
        var cache          = scope.ServiceProvider.GetRequiredService<IAizenDistributedCache>();

        var expired = await kits.GetExpiredUnmarkedAsync(ct);
        _logger.LogInformation("KitExpiredMarkingJob: marking {Count} kits as expired", expired.Count);

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

            _ = activationLogs.InsertAsync(logDoc, ct)
                .ContinueWith(
                    t => _logger.LogError(t.Exception, "Failed to write expiry log for Kit {KitId}", kit.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            await publisher.PublishAsync(new CargoDryKitExpiredMessage
            {
                KitId       = kit.Id,
                KitCode     = kit.KitCode,
                OwnerUserId = kit.OwnerUserId!.Value,
                VesselId    = kit.VesselId!.Value,
            }, ct);
        }

        await kits.SaveChangesAsync(ct);

        if (expired.Count > 0)
            await cache.RemoveAsync<CargoDryStatsDto>("cargodry:stats:global", ct);
    }
}
