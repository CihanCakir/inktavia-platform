using Aizen.Bff.AdminPanel.Application.ProfilePerformance.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ProfilePerformance.Query.GetProfilePerformanceDecisionLogs;

public sealed class GetProfilePerformanceDecisionLogsBffQuery
    : AizenQuery<GetProfilePerformanceDecisionLogsBffResponse>
{
    public long   ProfileId   { get; init; }
    public string ProfileType { get; init; } = "Provider";
    public int    Page        { get; init; } = 1;
    public int    PageSize    { get; init; } = 25;
}

public sealed class GetProfilePerformanceDecisionLogsBffResponse
{
    public ProfileDecisionLogPagedBffResultDto Data { get; init; } = default!;
}
