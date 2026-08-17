using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderCommissionBenefitRuleDetail;

[DocumentationInfo("Detail provider-commission-benefit rule BFF query handler (BE-P7)",
    "Returns a single benefit rule (GET /commission-benefits/rules/{id}). Read-only.")]
public sealed class GetProviderCommissionBenefitRuleDetailBffQueryHandler
    : AizenQueryHandler<GetProviderCommissionBenefitRuleDetailBffQuery, ProviderCommissionBenefitRuleDetailBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetProviderCommissionBenefitRuleDetailBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ProviderCommissionBenefitRuleDetailBffResponse?> Handle(GetProviderCommissionBenefitRuleDetailBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.GetProviderCommissionBenefitRuleDetailAsync(request.Id, ct) };
}
