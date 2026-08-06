using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySettlementAutomationRunsPaged;

[DocumentationInfo("GetCargoDrySettlementAutomationRunsPagedBffQueryHandler",
    "Proxies the paged settlement automation run history query to the CargoDry module. " +
    "All filter parameters are optional; results ordered by TriggeredAtUtc DESC. " +
    "RunItems are NOT included in list responses — use the detail endpoint for that. " +
    "Phase 6 (July 2026).")]
public sealed class GetCargoDrySettlementAutomationRunsPagedBffQueryHandler
    : AizenQueryHandler<GetCargoDrySettlementAutomationRunsPagedBffQuery,
                        GetCargoDrySettlementAutomationRunsPagedBffQueryResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDrySettlementAutomationRunsPagedBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDrySettlementAutomationRunsPagedBffQueryResponse> Handle(
        GetCargoDrySettlementAutomationRunsPagedBffQuery request, CancellationToken ct)
    {
        var pageSize = request.Take > 0 ? request.Take : 20;
        var page     = request.Take > 0 ? request.Skip / request.Take + 1 : 1;

        var result = await _remote.GetSettlementAutomationRunsAsync(
            targetYearMonth:   request.TargetYearMonth,
            status:            request.Status,
            mode:              request.Mode,
            triggeredByUserId: request.TriggeredByUserId,
            fromUtc:           request.FromUtc,
            toUtc:             request.ToUtc,
            page:              page,
            pageSize:          pageSize,
            ct:                ct);

        return new GetCargoDrySettlementAutomationRunsPagedBffQueryResponse { Result = result };
    }
}
