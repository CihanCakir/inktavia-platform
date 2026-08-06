using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ProfilePerformance.Query.GetProfilePerformanceRiskSignals;

public sealed class GetProfilePerformanceRiskSignalsBffQueryHandler
    : AizenQueryHandler<GetProfilePerformanceRiskSignalsBffQuery, GetProfilePerformanceRiskSignalsBffResponse>
{
    private readonly IProfilePerformanceRemoteCall _remote;

    public GetProfilePerformanceRiskSignalsBffQueryHandler(IProfilePerformanceRemoteCall remote)
        => _remote = remote;

    public override async Task<GetProfilePerformanceRiskSignalsBffResponse> Handle(
        GetProfilePerformanceRiskSignalsBffQuery request, CancellationToken ct)
    {
        var data = await _remote.GetRiskSignalsAsync(
            request.ProfileId, request.ProfileType,
            request.ActiveOnly, request.Page, request.PageSize, ct);
        return new GetProfilePerformanceRiskSignalsBffResponse { Data = data };
    }
}
