using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.WorkLog;

[DocumentationInfo("Get work logs query handler", "Maps work log entities and phases to BFF-friendly work logs response.")]
public sealed class GetWorkLogsQueryHandler : AizenQueryHandler<GetWorkLogsQuery, GetWorkLogsResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IWorkPhaseRepository _workPhaseRepository;
    private readonly IServiceRequestWorkLogRepository _workLogRepository;

    public GetWorkLogsQueryHandler(
        IServiceRequestRepository srRepository,
        IWorkPhaseRepository workPhaseRepository,
        IServiceRequestWorkLogRepository workLogRepository)
    {
        _srRepository = srRepository;
        _workPhaseRepository = workPhaseRepository;
        _workLogRepository = workLogRepository;
    }

    public override async Task<GetWorkLogsResponse> Handle(GetWorkLogsQuery request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var phases = await _workPhaseRepository.GetByServiceRequestIdAsync(request.ServiceRequestId, cancellationToken);

        var phaseDtos = phases.Select(p => new WorkPhaseSummaryDto
        {
            Number = p.PhaseNumber,
            Title = p.Title,
            ProgressPercent = p.ProgressPercent,
            Status = p.Status
        }).ToList();

        var currentPhase = phaseDtos.FirstOrDefault(p => p.Status == "In Progress")
            ?? phaseDtos.FirstOrDefault()
            ?? new WorkPhaseSummaryDto { Number = 1, Title = "Initial", ProgressPercent = 0, Status = "Upcoming" };

        var workLogs = await _workLogRepository.GetByServiceRequestIdAsync(request.ServiceRequestId, cancellationToken);

        var logEntries = workLogs.Select(l => new WorkLogEntryItemDto
        {
            Id = l.Id.ToString(),
            Timestamp = l.LoggedAt != default ? new DateTimeOffset(l.LoggedAt, TimeSpan.Zero) : DateTimeOffset.UtcNow,
            Type = l.LogType.ToString().ToLowerInvariant(),
            Content = l.Description ?? l.Title,
            MediaUrl = null,
            Author = l.ProviderUserId.ToString()
        }).ToList();

        var activities = workLogs.Select(l => new ProviderActivityEntryDto
        {
            Id = l.Id.ToString(),
            Timestamp = l.LoggedAt != default ? new DateTimeOffset(l.LoggedAt, TimeSpan.Zero) : DateTimeOffset.UtcNow,
            Description = l.Title,
            Icon = "check_circle"
        }).ToList();

        var assignmentProviderId = sr.Assignment?.ProviderUserId.ToString() ?? string.Empty;
        var providerActivity = new ProviderActivitySummaryDto
        {
            ProviderId = assignmentProviderId,
            ProviderName = sr.AssignedProviderName ?? string.Empty,
            AvatarUrl = null,
            Activities = activities
        };

        var jobHealth = ComputeJobHealth(sr, phaseDtos, workLogs.Count);

        return new GetWorkLogsResponse(
            sr.Id.ToString(), sr.Title, currentPhase, phaseDtos, logEntries, providerActivity, jobHealth);
    }

    private static JobHealthDto ComputeJobHealth(
        Domain.Entities.ServiceRequest.ServiceRequestEntity sr,
        List<WorkPhaseSummaryDto> phases,
        int logCount)
    {
        var daysRemaining = sr.RequestedEndDate.HasValue
            ? (int)(sr.RequestedEndDate.Value - DateTime.UtcNow).TotalDays
            : 0;
        daysRemaining = Math.Max(0, daysRemaining);

        // Short-circuit: terminal statuses override the phase score
        var srStatus = sr.Status.ToString();
        if (srStatus is "Completed" or "Cancelled")
        {
            return new JobHealthDto
            {
                Score            = srStatus == "Completed" ? 100 : 0,
                Label            = srStatus == "Completed" ? "Completed" : "Cancelled",
                DaysRemaining    = 0,
                MilestoneReached = phases.LastOrDefault(p => p.Status == "Completed")?.Title ?? string.Empty
            };
        }
        if (srStatus is "DisputeOpened")
        {
            return new JobHealthDto
            {
                Score            = 20,
                Label            = "Disputed",
                DaysRemaining    = daysRemaining,
                MilestoneReached = phases.LastOrDefault(p => p.Status == "Completed")?.Title ?? string.Empty
            };
        }
        if (srStatus is "CompletionSubmitted")
        {
            return new JobHealthDto
            {
                Score            = 90,
                Label            = "Pending Review",
                DaysRemaining    = daysRemaining,
                MilestoneReached = phases.LastOrDefault(p => p.Status == "Completed")?.Title ?? string.Empty
            };
        }

        // Dynamic score from phases
        int score;
        string label;
        if (phases.Count == 0)
        {
            // No phases defined: use log activity as a rough proxy
            score = logCount > 5 ? 65 : logCount > 0 ? 40 : 20;
            label = score >= 60 ? "Good" : "At Risk";
        }
        else
        {
            var completed   = phases.Count(p => p.Status == "Completed");
            var inProgress  = phases.Count(p => p.Status == "In Progress");
            // Each completed phase = full weight; in-progress = half weight
            var weightedDone = completed + inProgress * 0.5;
            score = (int)Math.Round(weightedDone / phases.Count * 100);

            // Penalise overdue
            if (sr.RequestedEndDate.HasValue && DateTime.UtcNow > sr.RequestedEndDate.Value)
                score = Math.Max(0, score - 20);

            label = score >= 80 ? "Good"
                  : score >= 50 ? "At Risk"
                  : "Critical";
        }

        return new JobHealthDto
        {
            Score            = score,
            Label            = label,
            DaysRemaining    = daysRemaining,
            MilestoneReached = phases.LastOrDefault(p => p.Status == "Completed")?.Title ?? string.Empty
        };
    }
}
