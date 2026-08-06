using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Query.GetProfilePerformanceSnapshot;

public sealed class GetProfilePerformanceSnapshotBffQueryHandler
    : AizenQueryHandler<GetProfilePerformanceSnapshotBffQuery, GetProfilePerformanceSnapshotBffResponse>
{
    private readonly IProfilePerformanceRemoteCall _remote;

    public GetProfilePerformanceSnapshotBffQueryHandler(IProfilePerformanceRemoteCall remote)
        => _remote = remote;

    public override async Task<GetProfilePerformanceSnapshotBffResponse> Handle(
        GetProfilePerformanceSnapshotBffQuery request, CancellationToken ct)
    {
        var data = await _remote.GetSnapshotAsync(request.ProfileId, request.ProfileType, ct);
        return new GetProfilePerformanceSnapshotBffResponse { Data = data };
    }
}
