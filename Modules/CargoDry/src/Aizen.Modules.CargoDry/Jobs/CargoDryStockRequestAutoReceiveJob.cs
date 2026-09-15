using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Application.Commands.ReceiveProviderStockRequest;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.CargoDry.Application.Jobs;

/// <summary>
/// Wave 4A — stock-request auto-receive sweep. Hourly, transitions Shipped requests whose frozen
/// AutoReceiveDeadlineUtc has elapsed to Received when the provider never confirmed. Idempotent + restart-safe:
/// the due-query only selects Shipped+overdue rows and the Receive command re-guards status == Shipped, so a
/// concurrent provider confirmation deterministically wins (this becomes a no-op for that row).
/// </summary>
public sealed class CargoDryStockRequestAutoReceiveJob : AizenRecurringJob
{
    private const int BatchSize = 200;

    public CargoDryStockRequestAutoReceiveJob(IAizenSchedulerLogger logger, IServiceProvider serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "25 * * * *"; // hourly at :25 (staggered from the SR supply sweeps)

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        IReadOnlyList<long> dueIds;
        using (var scope = ServiceProvider.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<ICargoDryStockRequestRepository>();
            dueIds = await repo.GetAutoReceiveDueIdsAsync(now, BatchSize, ct);
        }

        Logger.WriteConsole($"CargoDryStockRequestAutoReceiveJob: {dueIds.Count} shipped request(s) past the auto-receive deadline.");

        var received = 0;
        foreach (var id in dueIds)
        {
            try
            {
                using var scope = ServiceProvider.CreateScope();
                var cqrs = scope.ServiceProvider.GetRequiredService<IAizenCQRSProcessor>();
                // System path: no ProviderProfileId (skip ownership check), no ReceivedByUserId.
                await cqrs.ProcessAsync<CargoDryStockRequestDto>(
                    new ReceiveProviderStockRequestCommand { RequestId = id, ProviderProfileId = null, ReceivedByUserId = null }, ct);
                received++;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole($"CargoDryStockRequestAutoReceiveJob: request {id} failed: {ex.Message}");
            }
        }

        Logger.WriteConsole($"CargoDryStockRequestAutoReceiveJob: {received} request(s) auto-received.");
    }
}
