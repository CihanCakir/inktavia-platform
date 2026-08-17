using Aizen.Bff.AdminPanel.Application.ProfilePerformance.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ProfilePerformance.Query.GetProfilePerformanceHistory;

public sealed class GetProfilePerformanceHistoryBffQuery
    : AizenQuery<GetProfilePerformanceHistoryBffResponse>
{
    public long   ProfileId   { get; init; }
    public string ProfileType { get; init; } = "Provider";
    public int    Page        { get; init; } = 1;
    public int    PageSize    { get; init; } = 25;
}

public sealed class GetProfilePerformanceHistoryBffResponse
{
    public ProfileScoreHistoryPagedBffResultDto Data { get; init; } = default!;
}
