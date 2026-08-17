using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryAnalytics;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.CargoDry.Domain.MongoDocuments;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.CargoDry.Application.Jobs;

/// <summary>
/// Runs daily at 00:05 UTC. Reads the previous day's activation log documents from MongoDB,
/// aggregates them into a CargoDryKitUsageSnapshotDocument, and upserts into the snapshots collection.
/// </summary>
public sealed class DailySnapshotJob : AizenRecurringJob
{
    public DailySnapshotJob(
        IAizenSchedulerLogger logger,
        IServiceProvider      serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "5 0 * * *"; // 00:05 UTC every day

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope  = ServiceProvider.CreateScope();
        var logRepo      = scope.ServiceProvider.GetRequiredService<ICargoDryActivationLogRepository>();
        var snapshotRepo = scope.ServiceProvider.GetRequiredService<ICargoDrySnapshotRepository>();
        var cache        = scope.ServiceProvider.GetRequiredService<IAizenDistributedCache>();

        var dateKey = DateTimeOffset.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var logs    = await logRepo.GetByDateRangeAsync(dateKey, dateKey, null, cancellationToken);

        if (logs.Count == 0)
        {
            Logger.WriteConsole($"DailySnapshotJob: no logs for {dateKey}");
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

        await snapshotRepo.UpsertAsync(snapshot, cancellationToken);
        await cache.RemoveAsync<GetCargoDryAnalyticsResponse>("cargodry:analytics:snapshot", cancellationToken);

        Logger.WriteConsole($"DailySnapshotJob: upserted snapshot for {dateKey} ({logs.Count} log entries)");
    }
}
