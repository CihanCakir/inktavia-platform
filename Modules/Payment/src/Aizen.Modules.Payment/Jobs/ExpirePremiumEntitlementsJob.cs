using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.Payment.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Payment.Jobs;

/// <summary>
/// BE-P11 §13.9 — runs hourly. Transitions Active premium entitlements whose <c>ExpiresAt</c> has passed to Expired
/// (idempotent; the guarded <c>Expire()</c> no-ops anything already Expired/Revoked). Delegates to
/// <see cref="PremiumBoostService.ExpireDueAsync"/> so the logic is shared + unit-tested.
///
/// Configuration:
///   Payment:PremiumExpiryBatch (default 500) — max entitlements per run.
/// </summary>
public sealed class ExpirePremiumEntitlementsJob : AizenRecurringJob
{
    public ExpirePremiumEntitlementsJob(
        IAizenSchedulerLogger logger,
        IServiceProvider      serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "0 * * * *"; // every hour

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = ServiceProvider.CreateScope();
        var config  = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var service = scope.ServiceProvider.GetRequiredService<PremiumBoostService>();

        var batchSize = config.GetValue<int>("Payment:PremiumExpiryBatch", 500);
        var expired   = await service.ExpireDueAsync(DateTime.UtcNow, batchSize, cancellationToken);

        Logger.WriteConsole($"ExpirePremiumEntitlementsJob: expired {expired} premium entitlement(s).");
    }
}
