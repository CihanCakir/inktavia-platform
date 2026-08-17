namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;

public sealed class GetProviderJobsSummaryResponse
{
    public int Assigned { get; init; }
    public int Scheduled { get; init; }
    public int InProgress { get; init; }
    public int WaitingForOwnerApproval { get; init; }
    public int WaitingForMaterial { get; init; }
    public int Paused { get; init; }
    public int CompletionSubmitted { get; init; }
    public int Completed { get; init; }
    public int Active { get; init; }
    public int Total { get; init; }
}
