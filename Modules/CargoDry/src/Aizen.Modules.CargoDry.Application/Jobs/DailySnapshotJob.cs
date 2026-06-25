using Aizen.Core.Cache.Abstraction;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryAnalytics;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Jobs;

/// <summary>
/// Runs daily at 00:05 UTC. Reads the previous day's activation log documents from MongoDB,
/// aggregates them into a CargoDryKitUsageSnapshotDocument, and upserts into the snapshots collection.
/// </summary>
public sealed class DailySnapshotJob : BackgroundService
{
    private readonly IServiceScopeFactory      _scopeFactory;
    private readonly ILogger<DailySnapshotJob> _logger;

    public DailySnapshotJob(IServiceScopeFactory sf, ILogger<DailySnapshotJob> logger)
    {
        _scopeFactory = sf;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now      = DateTimeOffset.UtcNow;
            var next0005 = now.Date.AddDays(now.Hour >= 0 && now.Minute >= 5 ? 1 : 0).AddMinutes(5);
            var delay    = next0005 - now;
            await Task.Delay(delay, stoppingToken);

            _logger.LogInformation("DailySnapshotJob starting at {Time}", DateTimeOffset.UtcNow);
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "DailySnapshotJob failed"); }
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope  = _scopeFactory.CreateScope();
        var logRepo      = scope.ServiceProvider.GetRequiredService<ICargoDryActivationLogRepository>();
        var snapshotRepo = scope.ServiceProvider.GetRequiredService<ICargoDrySnapshotRepository>();
        var cache        = scope.ServiceProvider.GetRequiredService<IAizenDistributedCache>();

        var dateKey = DateTimeOffset.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var logs    = await logRepo.GetByDateRangeAsync(dateKey, dateKey, null, ct);

        if (logs.Count == 0)
        {
            _logger.LogInformation("DailySnapshotJob: no logs for {DateKey}", dateKey);
            return;
        }

        var byProduct = logs
            .Where(l => l.EventType == "Activated")
            .GroupBy(l => l.ProductCode)
            .Select(g => new ProductDaySnapshot
            {
                ProductCode = g.Key,
                ProductName = g.Key,
                Activations = g.Count(),
                Renewals    = logs.Count(l => l.EventType == "Renewed" && l.ProductCode == g.Key),
            })
            .ToList();

        var snapshot = new CargoDryKitUsageSnapshotDocument
        {
            Id          = dateKey,
            DateKey     = dateKey,
            ComputedAt  = DateTimeOffset.UtcNow,
            Activations = logs.Count(l => l.EventType == "Activated"),
            Renewals    = logs.Count(l => l.EventType == "Renewed"),
            Expirations = logs.Count(l => l.EventType == "Expired"),
            Revocations = logs.Count(l => l.EventType == "Revoked"),
            Extensions  = logs.Count(l => l.EventType == "Extended"),
            ByProduct   = byProduct,
            EfficiencyBuckets =
            [
                new EfficiencyBucketSnapshot { Bucket = "0-25",   Count = 0 },
                new EfficiencyBucketSnapshot { Bucket = "25-50",  Count = 0 },
                new EfficiencyBucketSnapshot { Bucket = "50-75",  Count = 0 },
                new EfficiencyBucketSnapshot { Bucket = "75-100", Count = 0 },
            ],
        };

        await snapshotRepo.UpsertAsync(snapshot, ct);
        await cache.RemoveAsync<GetCargoDryAnalyticsResponse>("cargodry:analytics:snapshot", ct);

        _logger.LogInformation("DailySnapshotJob: upserted snapshot for {DateKey} ({Count} log entries)", dateKey, logs.Count);
    }
}
