using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.FileStorage.Jobs;

/// <summary>
/// Publishes <see cref="OrphanFileCleanupRequestedMessage"/> once an hour so that
/// <c>OrphanFileCleanupConsumer</c> reaps files that were uploaded but never claimed (the user closed the tab)
/// and files whose claim was released when their document was deleted.
///
/// Without this job the consumer exists but nothing ever triggers it, so nothing is ever reaped and the bucket
/// grows without bound — the exact leak Phase 2h set out to close.
/// </summary>
public sealed class OrphanFileCleanupJob : AizenRecurringJob
{
    public OrphanFileCleanupJob(
        IAizenSchedulerLogger logger,
        IServiceProvider serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool IsActive => true;

    /// <summary>Hourly at :30 — the TTL (default 24 h) decides what is actually old enough to reap.</summary>
    public override string CronExpression => "30 * * * *";

    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = ServiceProvider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();

        Logger.WriteConsole("OrphanFileCleanupJob: requesting orphan file cleanup sweep");

        await publisher.PublishAsync(new OrphanFileCleanupRequestedMessage(), cancellationToken);
    }
}
