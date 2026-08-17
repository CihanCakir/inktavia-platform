using Aizen.Bff.AdminPanel.Application.ProfilePerformance.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ProfilePerformance.Query.GetProfilePerformanceRiskSignals;

public sealed class GetProfilePerformanceRiskSignalsBffQuery
    : AizenQuery<GetProfilePerformanceRiskSignalsBffResponse>
{
    public long   ProfileId   { get; init; }
    public string ProfileType { get; init; } = "Provider";
    public bool?  ActiveOnly  { get; init; }
    public int    Page        { get; init; } = 1;
    public int    PageSize    { get; init; } = 25;
}

public sealed class GetProfilePerformanceRiskSignalsBffResponse
{
    public ProfileRiskSignalPagedBffResultDto Data { get; init; } = default!;
}
