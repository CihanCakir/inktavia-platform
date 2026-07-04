using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryMonthlySettlementAutomationPreview;

[DocumentationInfo("Get CargoDry monthly settlement automation preview query handler",
    "Delegates to ICargoDryMonthlySettlementAutomationService.GetPreviewAsync. " +
    "Read-only — no mutations. Phase 6 (July 2026).")]
public sealed class GetCargoDryMonthlySettlementAutomationPreviewQueryHandler
    : AizenQueryHandler<GetCargoDryMonthlySettlementAutomationPreviewQuery,
                        GetCargoDryMonthlySettlementAutomationPreviewQueryResponse>
{
    private readonly ICargoDryMonthlySettlementAutomationService _automationService;

    public GetCargoDryMonthlySettlementAutomationPreviewQueryHandler(
        ICargoDryMonthlySettlementAutomationService automationService)
        => _automationService = automationService;

    public override async Task<GetCargoDryMonthlySettlementAutomationPreviewQueryResponse> Handle(
        GetCargoDryMonthlySettlementAutomationPreviewQuery request, CancellationToken ct)
    {
        var preview = await _automationService.GetPreviewAsync(
            targetYearMonth:    request.TargetYearMonth,
            autoPreparePayment: request.AutoPreparePayment,
            autoPrepareInvoice: request.AutoPrepareInvoice,
            ct:                 ct);

        return new GetCargoDryMonthlySettlementAutomationPreviewQueryResponse { Preview = preview };
    }
}
