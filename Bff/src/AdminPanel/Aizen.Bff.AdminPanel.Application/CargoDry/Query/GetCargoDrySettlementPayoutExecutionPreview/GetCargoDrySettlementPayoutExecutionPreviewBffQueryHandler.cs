using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDrySettlementPayoutExecutionPreview;

[DocumentationInfo("Get CargoDry settlement payout execution preview BFF query handler",
    "Proxies the payout-execution-preview call to the CargoDry commercial module. " +
    "Returns CanApprovePayout, CanMarkProcessing, CanCompletePayout, CanFailPayout flags " +
    "plus live payout status from the Payment module. " +
    "Safe to call at any time — never throws for business ineligibility. " +
    "Phase 4D (July 2026).")]
public sealed class GetCargoDrySettlementPayoutExecutionPreviewBffQueryHandler
    : AizenQueryHandler<GetCargoDrySettlementPayoutExecutionPreviewBffQuery,
                        GetCargoDrySettlementPayoutExecutionPreviewBffQueryResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDrySettlementPayoutExecutionPreviewBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDrySettlementPayoutExecutionPreviewBffQueryResponse> Handle(
        GetCargoDrySettlementPayoutExecutionPreviewBffQuery request, CancellationToken ct)
    {
        var preview = await _remote.GetSettlementPayoutExecutionPreviewAsync(
            request.SettlementId, ct);

        return new GetCargoDrySettlementPayoutExecutionPreviewBffQueryResponse
        {
            Preview = preview,
        };
    }
}
