using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Abstraction.Message.Performance;
using Aizen.Modules.Profile.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Profile.Jobs;

/// <summary>
/// Scheduled recurring job — finds Provider snapshots whose <c>LastCalculatedAtUtc</c>
/// is older than <c>ProfilePerformance:ScheduledRecompute:StaleThresholdHours</c>
/// (or null — never computed), then publishes <see cref="ProfilePerformanceRecomputeRequestedMessage"/>
/// for each one so that the score is recalculated asynchronously.
///
/// ── Phase 20H hard rules ──────────────────────────────────────────────────────
///   1. This job MUST NOT call ProfilePerformanceEngine directly.
///   2. This job MUST NOT calculate any scores.
///   3. This job publishes <see cref="ProfilePerformanceRecomputeRequestedMessage"/> only.
///   4. Actual score recalculation is handled by ProfilePerformanceRecomputeRequestedConsumer.
///   5. <see cref="IsActive"/>, cron expression, stale threshold and batch size are all
///      driven by IConfiguration — no hard-coded values used at runtime.
///
/// ── Config keys (appsettings.json → ProfilePerformance:ScheduledRecompute) ────
///   Enabled            : bool   — false disables the job without removing it
///   Cron               : string — Hangfire cron expression (default: 02:00 UTC daily)
///   StaleThresholdHours: int    — snapshot age in hours before recompute is triggered
///   BatchSize          : int    — max profiles to process per run
/// </summary>
public sealed class ProfilePerformanceScheduledRecomputeJob : AizenRecurringJob
{
    private readonly IConfiguration _config;

    public ProfilePerformanceScheduledRecomputeJob(
        IAizenSchedulerLogger logger,
        IServiceProvider      serviceProvider,
        IConfiguration        config)
        : base(logger, serviceProvider)
    {
        _config = config;
    }

    public override bool IsActive =>
        _config.GetValue<bool>("ProfilePerformance:ScheduledRecompute:Enabled", false);

    public override string CronExpression =>
        _config["ProfilePerformance:ScheduledRecompute:Cron"] ?? "0 2 * * *"; // 02:00 UTC daily

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        var staleHours = _config.GetValue<int>("ProfilePerformance:ScheduledRecompute:StaleThresholdHours", 24);
        var batchSize  = _config.GetValue<int>("ProfilePerformance:ScheduledRecompute:BatchSize", 100);

        using var scope  = ServiceProvider.CreateScope();
        var snapshots    = scope.ServiceProvider.GetRequiredService<IProfilePerformanceSnapshotRepository>();
        var publisher    = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();
        var log          = scope.ServiceProvider.GetRequiredService<ILogger<ProfilePerformanceScheduledRecomputeJob>>();

        var stale = await snapshots.GetProviderProfilesRequiringRecomputeAsync(staleHours, batchSize, ct);

        if (stale.Count == 0)
        {
            Logger.WriteConsole("ProfilePerformanceScheduledRecomputeJob: no stale snapshots found.");
            return;
        }

        Logger.WriteConsole(
            $"ProfilePerformanceScheduledRecomputeJob: {stale.Count} stale snapshot(s) found " +
            $"(threshold={staleHours}h, batchSize={batchSize}). Publishing recompute messages.");

        var published = 0;
        foreach (var snapshot in stale)
        {
            try
            {
                var message = new ProfilePerformanceRecomputeRequestedMessage
                {
                    ProfileId      = snapshot.ProfileId,
                    ProfileType    = (int)ProfileType.Provider,
                    TriggerReason  = "ScheduledRecompute",
                    SourceModule   = "Profile",
                    SourceEntityId = snapshot.ProfileId,
                    // Idempotency key is scoped to the UTC day — a profile recomputed multiple
                    // times in the same day by the scheduler is safe (upsert semantics in handler).
                    IdempotencyKey = $"SCHEDULED-{snapshot.ProfileId}-{DateTime.UtcNow:yyyyMMdd}",
                    RequestedAtUtc = DateTime.UtcNow,
                };

                await publisher.PublishAsync(message, ct);
                published++;
            }
            catch (Exception ex)
            {
                log.LogError(ex,
                    "ProfilePerformanceScheduledRecomputeJob: failed to publish for ProfileId={ProfileId}",
                    snapshot.ProfileId);
            }
        }

        Logger.WriteConsole(
            $"ProfilePerformanceScheduledRecomputeJob: published {published}/{stale.Count} recompute messages.");
        Logger.SetProgressBar(100);
    }
}
