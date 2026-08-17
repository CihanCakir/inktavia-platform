using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderCommissionBenefitEntitlementsList;

[DocumentationInfo("List provider-commission-benefit entitlements BFF query handler (BE-P7)",
    "Lists granted entitlements (GET /commission-benefits/entitlements) with optional filters. Empty-list fallback on null.")]
public sealed class GetProviderCommissionBenefitEntitlementsListBffQueryHandler
    : AizenQueryHandler<GetProviderCommissionBenefitEntitlementsListBffQuery, ProviderCommissionBenefitEntitlementListBffResult>
{
    private readonly IPaymentRemoteCall _payment;
    public GetProviderCommissionBenefitEntitlementsListBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ProviderCommissionBenefitEntitlementListBffResult?> Handle(GetProviderCommissionBenefitEntitlementsListBffQuery request, CancellationToken ct)
        => await _payment.ListProviderCommissionBenefitEntitlementsAsync(
               request.ProviderProfileId, request.BenefitRuleId, request.IsActive, ct)
           ?? new ProviderCommissionBenefitEntitlementListBffResult(new(), 0);
}
