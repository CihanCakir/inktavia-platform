namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;

public sealed class GetProviderJobsWorkloadResponse
{
    public List<WorkloadWeekBucket> Weeks { get; init; } = new();
}

public sealed class WorkloadWeekBucket
{
    public DateTime WeekStartUtc { get; init; }
    public string Label { get; init; } = string.Empty;
    public int Scheduled { get; init; }
    public int InProgress { get; init; }
    public int Completed { get; init; }
}
