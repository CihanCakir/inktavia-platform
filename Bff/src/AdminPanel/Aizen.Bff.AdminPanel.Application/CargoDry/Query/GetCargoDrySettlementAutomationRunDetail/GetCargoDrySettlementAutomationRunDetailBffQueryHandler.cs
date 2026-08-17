using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDrySettlementAutomationRunDetail;

[DocumentationInfo("GetCargoDrySettlementAutomationRunDetailBffQueryHandler",
    "Proxies the settlement automation run detail query to the CargoDry module. " +
    "Includes all per-settlement RunItems in the response. " +
    "Phase 6 (July 2026).")]
public sealed class GetCargoDrySettlementAutomationRunDetailBffQueryHandler
    : AizenQueryHandler<GetCargoDrySettlementAutomationRunDetailBffQuery,
                        GetCargoDrySettlementAutomationRunDetailBffQueryResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDrySettlementAutomationRunDetailBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDrySettlementAutomationRunDetailBffQueryResponse> Handle(
        GetCargoDrySettlementAutomationRunDetailBffQuery request, CancellationToken ct)
    {
        var run = await _remote.GetSettlementAutomationRunDetailAsync(request.RunId, ct);

        return new GetCargoDrySettlementAutomationRunDetailBffQueryResponse { Run = run };
    }
}
