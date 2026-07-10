namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;

[DocumentationInfo("Get provider jobs response", "A provider's own assignments (jobs), scoped to the caller's provider profile.")]
public sealed class GetProviderJobsResponse
{
    public List<ProviderJobItemDto> Items { get; init; } = new();
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
    public int TotalReturned => Items.Count;
}

public sealed record ProviderJobItemDto
{
    public long AssignmentId { get; init; }
    public long ServiceRequestId { get; init; }
    public long ServiceRequestOfferId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? ScheduledStartDate { get; init; }
    public DateTime? ScheduledEndDate { get; init; }
    public DateTime? ActualStartDate { get; init; }
    public DateTime? ActualEndDate { get; init; }
    public string? ProviderNotes { get; init; }
}
