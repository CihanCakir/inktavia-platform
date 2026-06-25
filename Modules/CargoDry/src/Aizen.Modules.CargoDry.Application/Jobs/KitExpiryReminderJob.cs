using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Jobs;

public sealed class KitExpiryReminderJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KitExpiryReminderJob> _logger;
    private static readonly int[] ReminderDays = [30, 7, 1];

    public KitExpiryReminderJob(IServiceScopeFactory sf, ILogger<KitExpiryReminderJob> logger)
    {
        _scopeFactory = sf;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now     = DateTimeOffset.UtcNow;
            var next9am = now.Date.AddDays(now.Hour >= 9 ? 1 : 0).AddHours(9);
            var delay   = next9am - now;
            await Task.Delay(delay, stoppingToken);

            _logger.LogInformation("KitExpiryReminderJob starting at {Time}", DateTimeOffset.UtcNow);
            try { await RunAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "KitExpiryReminderJob failed"); }
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var kits        = scope.ServiceProvider.GetRequiredService<ICargoDryKitRepository>();
        var publisher   = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();

        foreach (var days in ReminderDays)
        {
            var expiring = await kits.GetExpiringAsync(days, ct);
            foreach (var kit in expiring.Where(k => k.DaysUntilExpiry == days))
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
                }, ct);
            }
        }
    }
}
