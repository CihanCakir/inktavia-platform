using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest.CompleteCargoDrySupplyOrder;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.ServiceRequest.Jobs;

/// <summary>
/// CargoDry supply v2 — auto-complete sweep. Hourly, completes supply orders past their frozen auto-complete deadline:
/// delivered-and-overdue (Assigned, provider path → attribution + commission) and shipped-and-overdue (Shipped, cargo
/// path → direct-sale). Completion is in-process (no token); it publishes escrow-release + sale-record messages that
/// Payment and CargoDry consume in-process. Idempotent + restart-safe: the completion command no-ops on terminal
/// states, so a late owner QR scan or overlapping run never double-processes.
/// </summary>
public sealed class CargoDrySupplyAutoCompleteJob : AizenRecurringJob
{
    private const int BatchSize = 200;

    public CargoDrySupplyAutoCompleteJob(IAizenSchedulerLogger logger, IServiceProvider serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "45 * * * *"; // hourly at :45 (staggered from the accept-timeout sweep)

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        IReadOnlyList<long> dueIds;
        using (var scope = ServiceProvider.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IServiceRequestRepository>();
            dueIds = await repo.GetCargoDrySupplyAutoCompleteDueIdsAsync(now, BatchSize, ct);
        }

        Logger.WriteConsole($"CargoDrySupplyAutoCompleteJob: {dueIds.Count} order(s) due to auto-complete.");

        var completed = 0;
        foreach (var id in dueIds)
        {
            try
            {
                using var scope = ServiceProvider.CreateScope();
                var cqrs = scope.ServiceProvider.GetRequiredService<IAizenCQRSProcessor>();
                var result = await cqrs.ProcessAsync<CompleteCargoDrySupplyOrderResponse>(
                    new CompleteCargoDrySupplyOrderCommand { ServiceRequestId = id, Note = "Auto-completed (window elapsed)" }, ct);
                if (result?.Completed == true) completed++;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole($"CargoDrySupplyAutoCompleteJob: SR {id} failed: {ex.Message}");
            }
        }

        Logger.WriteConsole($"CargoDrySupplyAutoCompleteJob: {completed} order(s) auto-completed.");
    }
}
