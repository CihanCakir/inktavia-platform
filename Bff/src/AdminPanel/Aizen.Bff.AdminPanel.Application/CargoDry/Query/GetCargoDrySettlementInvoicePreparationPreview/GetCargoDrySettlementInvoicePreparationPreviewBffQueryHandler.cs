using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDrySettlementInvoicePreparationPreview;

[DocumentationInfo("Get CargoDry settlement invoice preparation preview BFF query handler",
    "Proxies the eligibility-preview call to the CargoDry commercial module. " +
    "Returns CanPrepare, BlockingReasons, financial summary, and existing invoice links. " +
    "Safe to call at any time — never throws for business ineligibility. " +
    "Phase 4C (July 2026).")]
public sealed class GetCargoDrySettlementInvoicePreparationPreviewBffQueryHandler
    : AizenQueryHandler<GetCargoDrySettlementInvoicePreparationPreviewBffQuery,
                        GetCargoDrySettlementInvoicePreparationPreviewBffQueryResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDrySettlementInvoicePreparationPreviewBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDrySettlementInvoicePreparationPreviewBffQueryResponse> Handle(
        GetCargoDrySettlementInvoicePreparationPreviewBffQuery request, CancellationToken ct)
    {
        var preview = await _remote.GetSettlementInvoicePreparationPreviewAsync(
            request.SettlementId, ct);

        return new GetCargoDrySettlementInvoicePreparationPreviewBffQueryResponse
        {
            Preview = preview,
        };
    }
}
