using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Query.GetProfilePerformanceComponents;

public sealed class GetProfilePerformanceComponentsBffQueryHandler
    : AizenQueryHandler<GetProfilePerformanceComponentsBffQuery, GetProfilePerformanceComponentsBffResponse>
{
    private readonly IProfilePerformanceRemoteCall _remote;

    public GetProfilePerformanceComponentsBffQueryHandler(IProfilePerformanceRemoteCall remote)
        => _remote = remote;

    public override async Task<GetProfilePerformanceComponentsBffResponse> Handle(
        GetProfilePerformanceComponentsBffQuery request, CancellationToken ct)
    {
        var data = await _remote.GetScoreComponentsAsync(request.ProfileId, request.ProfileType, ct);
        return new GetProfilePerformanceComponentsBffResponse { Data = data };
    }
}
