using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryStats;

[DocumentationInfo("Get CargoDry stats BFF query handler", "Calls the CargoDry admin stats endpoint and returns aggregated kit statistics.")]
public sealed class GetCargoDryStatsBffQueryHandler
    : AizenQueryHandler<GetCargoDryStatsBffQuery, GetCargoDryStatsBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryStatsBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryStatsBffResponse> Handle(
        GetCargoDryStatsBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetStatsAsync(ct);
        return new GetCargoDryStatsBffResponse { Stats = result };
    }
}
