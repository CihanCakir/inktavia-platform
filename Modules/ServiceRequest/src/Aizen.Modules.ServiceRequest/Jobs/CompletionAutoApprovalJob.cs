using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Application.Command.Completion;
using Aizen.Modules.ServiceRequest.Application.Completion;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.ServiceRequest.Jobs;

/// <summary>
/// N3-C — the completion auto-approval job. Runs hourly and, for still-pending (Submitted) completions with a set
/// <c>AutoApproveAt</c>:
///   1. within the reminder-lead window and not yet reminded → emit <c>CompletionAutoApproveApproaching</c> (owner
///      nudge) once and stamp <c>AutoApproveReminderSentAt</c>;
///   2. past the deadline → approve through the <b>existing</b> <see cref="ApproveServiceRequestCompletionCommand"/>
///      with a system actor, so the exact same escrow-release / settlement consequences fire (no parallel approval
///      that skips payment). Owner action before the deadline flips the status out of Submitted → the query excludes it
///      (and the approval command no-ops), so the auto-approval is cancelled.
///
/// Multi-replica safety: the recurring trigger fires once cluster-wide (Hangfire's storage-backed distributed lock),
/// and per-item exactly-once is enforced by the DB status transition (Submitted → ApprovedByOwner is one-way, guarded
/// in the approval handler) + the <c>AutoApproveReminderSentAt</c> once-guard. Each item runs in its own scope.
/// </summary>
public sealed class CompletionAutoApprovalJob : AizenRecurringJob
{
    public CompletionAutoApprovalJob(IAizenSchedulerLogger logger, IServiceProvider serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "30 * * * *"; // hourly at :30

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        int reminderLead, batch, windowDays;
        long systemActor;
        using (var scope = ServiceProvider.CreateScope())
        {
            var config   = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            reminderLead = CompletionAutoApprovalOptions.ReminderLeadDays(config);
            batch        = CompletionAutoApprovalOptions.BatchSize(config);
            windowDays   = CompletionAutoApprovalOptions.WindowDays(config);
            systemActor  = CompletionAutoApprovalOptions.SystemActorUserId(config);
        }

        var now    = DateTime.UtcNow;
        var cutoff = now.AddDays(reminderLead);   // fetch both approaching-reminder and past-deadline candidates

        List<(long CompletionId, long ServiceRequestId, DateTime AutoApproveAt, bool ReminderSent)> candidates;
        using (var scope = ServiceProvider.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IServiceRequestCompletionRepository>();
            candidates = (await repo.GetPendingAutoApproveCandidatesAsync(cutoff, batch, ct))
                .Select(c => (c.Id, c.ServiceRequestId, c.AutoApproveAt!.Value, c.AutoApproveReminderSentAt is not null))
                .ToList();
        }

        Logger.WriteConsole($"CompletionAutoApprovalJob: {candidates.Count} candidate completion(s).");

        var approved = 0; var reminded = 0;
        foreach (var c in candidates)
        {
            try
            {
                if (now >= c.AutoApproveAt)
                {
                    await AutoApproveAsync(c.ServiceRequestId, systemActor, windowDays, ct);
                    approved++;
                }
                else if (!c.ReminderSent)
                {
                    await SendReminderAsync(c.ServiceRequestId, now, ct);
                    reminded++;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteConsole($"CompletionAutoApprovalJob: item SR {c.ServiceRequestId} failed: {ex.Message}");
            }
        }

        Logger.WriteConsole($"CompletionAutoApprovalJob: auto-approved {approved}, reminded {reminded}.");
    }

    // Reuse the exact owner-approval command (system actor) — same status transition + ServiceRequestCompletionApprovedMessage
    // + downstream escrow release. The handler no-ops if the completion is no longer Submitted (owner acted first).
    private async Task AutoApproveAsync(long serviceRequestId, long systemActor, int windowDays, CancellationToken ct)
    {
        using var scope = ServiceProvider.CreateScope();
        var cqrs = scope.ServiceProvider.GetRequiredService<IAizenCQRSProcessor>();
        await cqrs.ProcessAsync<Abstraction.Response.Completion.ApproveServiceRequestCompletionResponse>(
            new ApproveServiceRequestCompletionCommand(
                serviceRequestId,
                new ApproveServiceRequestCompletionRequest
                {
                    ReviewNotes = $"Auto-approved by system after {windowDays} days without owner review.",
                },
                actingUserIdOverride: systemActor,
                actorTypeOverride:    ServiceRequestActorType.System),
            ct);
    }

    private async Task SendReminderAsync(long serviceRequestId, DateTime now, CancellationToken ct)
    {
        using var scope = ServiceProvider.CreateScope();
        var completionRepo = scope.ServiceProvider.GetRequiredService<IServiceRequestCompletionRepository>();
        var srRepo         = scope.ServiceProvider.GetRequiredService<IServiceRequestRepository>();
        var publisher      = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();

        var completion = await completionRepo.GetByServiceRequestIdAsync(serviceRequestId, ct);
        // Re-check under this scope: still pending, deadline set, not yet reminded, and not already past due.
        if (completion is null
            || completion.Status != ServiceRequestCompletionStatus.Submitted
            || completion.AutoApproveAt is not { } deadline
            || completion.AutoApproveReminderSentAt is not null
            || now >= deadline)
            return;

        var sr = await srRepo.GetByIdAsync(serviceRequestId, ct);
        if (sr is null) return;

        var daysRemaining = Math.Max(1, (int)Math.Ceiling((deadline - now).TotalDays));

        await publisher.PublishAsync(new ServiceRequestCompletionAutoApproveApproachingMessage
        {
            ServiceRequestId = sr.Id,
            CompletionId     = completion.Id,
            OwnerUserId      = sr.OwnerUserId,
            AutoApproveAtUtc = deadline,
            DaysRemaining    = daysRemaining,
        }, ct);

        completion.MarkAutoApproveReminderSent();
        completionRepo.Update(completion);
        await completionRepo.SaveChangesAsync(ct);
    }
}
