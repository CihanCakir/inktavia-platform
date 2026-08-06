using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySettlementAutomationPreview;

[DocumentationInfo("GetCargoDrySettlementAutomationPreviewBffQueryHandler",
    "Proxies the settlement automation preview call to the CargoDry module. " +
    "Returns a read-only prediction of what the automation run WOULD do for the target year-month " +
    "without mutating any settlement. Phase 6 (July 2026).")]
public sealed class GetCargoDrySettlementAutomationPreviewBffQueryHandler
    : AizenQueryHandler<GetCargoDrySettlementAutomationPreviewBffQuery,
                        GetCargoDrySettlementAutomationPreviewBffQueryResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDrySettlementAutomationPreviewBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDrySettlementAutomationPreviewBffQueryResponse> Handle(
        GetCargoDrySettlementAutomationPreviewBffQuery request, CancellationToken ct)
    {
        var preview = await _remote.GetSettlementAutomationPreviewAsync(
            targetYearMonth:    request.TargetYearMonth,
            autoPreparePayment: request.AutoPreparePayment,
            autoPrepareInvoice: request.AutoPrepareInvoice,
            ct:                 ct);

        return new GetCargoDrySettlementAutomationPreviewBffQueryResponse { Preview = preview };
    }
}
