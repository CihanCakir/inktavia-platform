using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDrySettlementPaymentPreparationPreview;

[DocumentationInfo("Get CargoDry settlement payment preparation preview BFF query handler",
    "Proxies the eligibility-preview call to the CargoDry commercial module. " +
    "Returns CanPrepare, BlockingReasons, attribution counts, and existing payout links. " +
    "Safe to call at any time — never throws for business ineligibility. " +
    "Phase 4B (July 2026).")]
public sealed class GetCargoDrySettlementPaymentPreparationPreviewBffQueryHandler
    : AizenQueryHandler<GetCargoDrySettlementPaymentPreparationPreviewBffQuery,
                        GetCargoDrySettlementPaymentPreparationPreviewBffQueryResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDrySettlementPaymentPreparationPreviewBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDrySettlementPaymentPreparationPreviewBffQueryResponse> Handle(
        GetCargoDrySettlementPaymentPreparationPreviewBffQuery request, CancellationToken ct)
    {
        var preview = await _remote.GetSettlementPaymentPreparationPreviewAsync(
            request.SettlementId, ct);

        return new GetCargoDrySettlementPaymentPreparationPreviewBffQueryResponse
        {
            Preview = preview,
        };
    }
}
