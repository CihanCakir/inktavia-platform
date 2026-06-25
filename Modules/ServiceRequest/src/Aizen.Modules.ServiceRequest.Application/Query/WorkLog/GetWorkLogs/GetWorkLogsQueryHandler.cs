using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
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

        var jobHealth = ComputeJobHealth(sr, workLogs.Count);

        return new GetWorkLogsResponse(
            sr.Id.ToString(), sr.Title, currentPhase, phaseDtos, logEntries, providerActivity, jobHealth);
    }

    private static JobHealthDto ComputeJobHealth(
        Domain.Entities.ServiceRequest.ServiceRequestEntity sr, int logCount)
    {
        var daysRemaining = sr.RequestedEndDate.HasValue
            ? (int)(sr.RequestedEndDate.Value - DateTime.UtcNow).TotalDays
            : 0;
        daysRemaining = Math.Max(0, daysRemaining);

        return new JobHealthDto
        {
            Score = 75,
            Label = "Good",
            DaysRemaining = daysRemaining,
            MilestoneReached = logCount.ToString()
        };
    }
}
