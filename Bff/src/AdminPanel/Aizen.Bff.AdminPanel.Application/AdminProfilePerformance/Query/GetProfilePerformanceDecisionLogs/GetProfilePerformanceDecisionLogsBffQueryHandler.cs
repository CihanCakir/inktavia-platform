using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Query.GetProfilePerformanceDecisionLogs;

public sealed class GetProfilePerformanceDecisionLogsBffQueryHandler
    : AizenQueryHandler<GetProfilePerformanceDecisionLogsBffQuery, GetProfilePerformanceDecisionLogsBffResponse>
{
    private readonly IAdminProfilePerformanceBffRemoteCall _remote;

    public GetProfilePerformanceDecisionLogsBffQueryHandler(IAdminProfilePerformanceBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetProfilePerformanceDecisionLogsBffResponse> Handle(
        GetProfilePerformanceDecisionLogsBffQuery request, CancellationToken ct)
    {
        var data = await _remote.GetDecisionLogsAsync(
            request.ProfileId, request.ProfileType, request.Page, request.PageSize, ct);
        return new GetProfilePerformanceDecisionLogsBffResponse { Data = data };
    }
}
