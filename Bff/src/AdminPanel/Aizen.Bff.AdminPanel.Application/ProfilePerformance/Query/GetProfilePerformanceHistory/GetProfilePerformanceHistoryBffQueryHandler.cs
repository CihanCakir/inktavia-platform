using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ProfilePerformance.Query.GetProfilePerformanceHistory;

public sealed class GetProfilePerformanceHistoryBffQueryHandler
    : AizenQueryHandler<GetProfilePerformanceHistoryBffQuery, GetProfilePerformanceHistoryBffResponse>
{
    private readonly IProfilePerformanceRemoteCall _remote;

    public GetProfilePerformanceHistoryBffQueryHandler(IProfilePerformanceRemoteCall remote)
        => _remote = remote;

    public override async Task<GetProfilePerformanceHistoryBffResponse> Handle(
        GetProfilePerformanceHistoryBffQuery request, CancellationToken ct)
    {
        var data = await _remote.GetScoreHistoryAsync(
            request.ProfileId, request.ProfileType, request.Page, request.PageSize, ct);
        return new GetProfilePerformanceHistoryBffResponse { Data = data };
    }
}
