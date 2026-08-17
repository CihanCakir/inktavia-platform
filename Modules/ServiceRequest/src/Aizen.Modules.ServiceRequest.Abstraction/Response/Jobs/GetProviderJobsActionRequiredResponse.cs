namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;

public sealed class GetProviderJobsActionRequiredResponse
{
    public List<ActionRequiredJobDto> OwnerApproval { get; init; } = new();
    public List<ActionRequiredJobDto> MaterialRequired { get; init; } = new();
    public List<ActionRequiredJobDto> Blocked { get; init; } = new();
}

public sealed class ActionRequiredJobDto
{
    public long AssignmentId { get; init; }
    public long ServiceRequestId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? RequestCode { get; init; }
    public long VesselId { get; init; }
    public string? VesselName { get; init; }
    public DateTime? ScheduledStartDate { get; init; }
}
