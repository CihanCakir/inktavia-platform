using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Query.GetProfilePerformanceSnapshotsByTier;

public sealed class GetProfilePerformanceSnapshotsByTierBffQueryHandler
    : AizenQueryHandler<GetProfilePerformanceSnapshotsByTierBffQuery, GetProfilePerformanceSnapshotsByTierBffResponse>
{
    private readonly IAdminProfilePerformanceBffRemoteCall _remote;

    public GetProfilePerformanceSnapshotsByTierBffQueryHandler(IAdminProfilePerformanceBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetProfilePerformanceSnapshotsByTierBffResponse> Handle(
        GetProfilePerformanceSnapshotsByTierBffQuery request, CancellationToken ct)
    {
        var data = await _remote.GetSnapshotsByTierAsync(request.Tier, request.Page, request.PageSize, ct);
        return new GetProfilePerformanceSnapshotsByTierBffResponse { Data = data };
    }
}
