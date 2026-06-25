using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Jobs;

public sealed class KitExpiredMarkingJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
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
        using var scope = _scopeFactory.CreateScope();
        var kits        = scope.ServiceProvider.GetRequiredService<ICargoDryKitRepository>();
        var publisher   = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();

        var expired = await kits.GetExpiredUnmarkedAsync(ct);
        _logger.LogInformation("KitExpiredMarkingJob: marking {Count} kits as expired", expired.Count);

        foreach (var kit in expired)
        {
            kit.MarkExpired();
            await publisher.PublishAsync(new CargoDryKitExpiredMessage
            {
                KitId       = kit.Id,
                KitCode     = kit.KitCode,
                OwnerUserId = kit.OwnerUserId!.Value,
                VesselId    = kit.VesselId!.Value,
            }, ct);
        }

        await kits.SaveChangesAsync(ct);
    }
}
