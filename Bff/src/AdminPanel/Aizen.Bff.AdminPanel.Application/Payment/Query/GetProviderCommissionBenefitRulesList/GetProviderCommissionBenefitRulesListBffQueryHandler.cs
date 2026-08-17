using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderCommissionBenefitRulesList;

[DocumentationInfo("List provider-commission-benefit rules BFF query handler (BE-P7)",
    "Lists benefit rules (GET /commission-benefits/rules) with optional filters. Falls back to an empty list if the module returns null.")]
public sealed class GetProviderCommissionBenefitRulesListBffQueryHandler
    : AizenQueryHandler<GetProviderCommissionBenefitRulesListBffQuery, ProviderCommissionBenefitRuleListBffResult>
{
    private readonly IPaymentRemoteCall _payment;
    public GetProviderCommissionBenefitRulesListBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ProviderCommissionBenefitRuleListBffResult?> Handle(GetProviderCommissionBenefitRulesListBffQuery request, CancellationToken ct)
        => await _payment.ListProviderCommissionBenefitRulesAsync(
               request.ProviderProfileId, request.ProviderPlanId, request.CategoryCode,
               request.CurrencyCode, request.Stackable, request.IsActive, ct)
           ?? new ProviderCommissionBenefitRuleListBffResult(new(), 0);
}
