using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Application.Maintenance;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.ServiceRequest.Jobs;

/// <summary>
/// N2 — the daily maintenance due-reminder job (08:00 UTC). Scans active schedules whose reminder window has opened
/// (now ≥ NextDueAt − ReminderLeadDays) and that haven't been reminded this cycle (ReminderSentAt is null); for each
/// it publishes a <see cref="MaintenanceReminderDueMessage"/> (→ Notification N-B path) and stamps
/// <c>ReminderSentAt = now</c>.
///
/// <para>Idempotent + multi-replica safe: the recurring trigger fires once cluster-wide (scheduler distributed lock),
/// and the per-item <c>ReminderSentAt</c> marker (re-checked under each item's own scope) guarantees exactly one
/// reminder per cycle — a same-day re-run re-selects nothing. The next reminder only after <c>RecordPerformed</c>
/// re-arms the schedule. Separate from CargoDry renewal.</para>
/// </summary>
public sealed class MaintenanceReminderDueJob : AizenRecurringJob
{
    public MaintenanceReminderDueJob(IAizenSchedulerLogger logger, IServiceProvider serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "0 8 * * *"; // 08:00 UTC every day

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        int batch;
        using (var scope = ServiceProvider.CreateScope())
        {
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            batch = MaintenanceScheduleOptions.ReminderBatchSize(config);
        }

        var now = DateTime.UtcNow;

        List<long> dueIds;
        using (var scope = ServiceProvider.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IMaintenanceScheduleRepository>();
            dueIds = (await repo.GetDueForReminderAsync(now, batch, ct)).Select(s => s.Id).ToList();
        }

        Logger.WriteConsole($"MaintenanceReminderDueJob: {dueIds.Count} schedule(s) in the reminder window.");

        var sent = 0;
        foreach (var id in dueIds)
        {
            try
            {
                using var scope = ServiceProvider.CreateScope();
                var repo      = scope.ServiceProvider.GetRequiredService<IMaintenanceScheduleRepository>();
                var publisher = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();

                // Re-check under this scope: still active, still un-armed, still inside the window (marker once-guard).
                var schedule = await repo.GetByIdAsync(id, ct);
                if (schedule is null || !schedule.NeedsReminder(now))
                    continue;

                await publisher.PublishAsync(new MaintenanceReminderDueMessage
                {
                    ScheduleId          = schedule.Id,
                    OwnerUserId         = schedule.OwnerUserId,
                    VesselId            = schedule.VesselId,
                    VesselName          = null,   // enriched by the FE / a vessel lookup follow-up; consumer falls back to "#{id}"
                    ServiceCategoryCode = schedule.ServiceCategoryCode,
                    ServiceTypeCode     = schedule.ServiceTypeCode,
                    NextDueAt           = schedule.NextDueAt,
                }, ct);

                schedule.MarkReminderSent(now);
                repo.Update(schedule);
                await repo.SaveChangesAsync(ct);
                sent++;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole($"MaintenanceReminderDueJob: schedule {id} failed: {ex.Message}");
            }
        }

        Logger.WriteConsole($"MaintenanceReminderDueJob: published {sent} reminder(s).");
    }
}
