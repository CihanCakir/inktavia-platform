using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest.FallbackCargoDrySupplyToCargo;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.ServiceRequest.Jobs;

/// <summary>
/// CargoDry supply v2 — accept-timeout sweep. Hourly, converts open CARGODRY_SUPPLY orders whose provider-accept window
/// has elapsed into direct cargo sales (AwaitingShipment). Idempotent + restart-safe: the due-query only selects Open
/// orders and the fallback command re-guards status==Open under its own scope (deterministic winner vs a provider
/// accept that flipped the order to Assigned). Cluster-single-fire via the scheduler's distributed lock.
/// </summary>
public sealed class CargoDrySupplyAcceptTimeoutJob : AizenRecurringJob
{
    private const int BatchSize = 200;

    public CargoDrySupplyAcceptTimeoutJob(IAizenSchedulerLogger logger, IServiceProvider serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "15 * * * *"; // hourly at :15

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        IReadOnlyList<long> dueIds;
        using (var scope = ServiceProvider.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IServiceRequestRepository>();
            dueIds = await repo.GetCargoDrySupplyAcceptTimedOutIdsAsync(now, BatchSize, ct);
        }

        Logger.WriteConsole($"CargoDrySupplyAcceptTimeoutJob: {dueIds.Count} order(s) past the accept window.");

        var fellBack = 0;
        foreach (var id in dueIds)
        {
            try
            {
                using var scope = ServiceProvider.CreateScope();
                var cqrs = scope.ServiceProvider.GetRequiredService<IAizenCQRSProcessor>();
                if (await cqrs.ProcessAsync<bool>(new FallbackCargoDrySupplyToCargoCommand { ServiceRequestId = id }, ct))
                    fellBack++;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole($"CargoDrySupplyAcceptTimeoutJob: SR {id} failed: {ex.Message}");
            }
        }

        Logger.WriteConsole($"CargoDrySupplyAcceptTimeoutJob: {fellBack} order(s) moved to cargo (AwaitingShipment).");
    }
}
