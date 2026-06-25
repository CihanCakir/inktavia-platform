using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

[DocumentationInfo("Get work logs response", "Work phases, log entries, provider activity and job health for a service request.")]
public sealed class GetWorkLogsResponse(
    string serviceRequestId,
    string title,
    WorkPhaseSummaryDto currentPhase,
    List<WorkPhaseSummaryDto> phases,
    List<WorkLogEntryItemDto> logEntries,
    ProviderActivitySummaryDto providerActivity,
    JobHealthDto jobHealth)
{
    public string ServiceRequestId { get; } = serviceRequestId;
    public string Title { get; } = title;
    public WorkPhaseSummaryDto CurrentPhase { get; } = currentPhase;
    public List<WorkPhaseSummaryDto> Phases { get; } = phases;
    public List<WorkLogEntryItemDto> LogEntries { get; } = logEntries;
    public ProviderActivitySummaryDto ProviderActivity { get; } = providerActivity;
    public JobHealthDto JobHealth { get; } = jobHealth;
}

public sealed record WorkPhaseSummaryDto
{
    public int Number { get; init; }
    public string Title { get; init; } = string.Empty;
    public int ProgressPercent { get; init; }
    public string Status { get; init; } = "Upcoming";
}

public sealed record WorkLogEntryItemDto
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string Type { get; init; } = "note";
    public string Content { get; init; } = string.Empty;
    public string? MediaUrl { get; init; }
    public string Author { get; init; } = string.Empty;
}

public sealed record ProviderActivitySummaryDto
{
    public string ProviderId { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public List<ProviderActivityEntryDto> Activities { get; init; } = [];
}

public sealed record ProviderActivityEntryDto
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = "check_circle";
}

public sealed record JobHealthDto
{
    public int Score { get; init; }
    public string Label { get; init; } = string.Empty;
    public int DaysRemaining { get; init; }
    public string MilestoneReached { get; init; } = string.Empty;
}
