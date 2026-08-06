using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderCommissionBenefitEntitlementDetail;

[DocumentationInfo("Detail provider-commission-benefit entitlement BFF query handler (BE-P7)",
    "Returns a single entitlement (GET /commission-benefits/entitlements/{id}). Read-only.")]
public sealed class GetProviderCommissionBenefitEntitlementDetailBffQueryHandler
    : AizenQueryHandler<GetProviderCommissionBenefitEntitlementDetailBffQuery, ProviderCommissionBenefitEntitlementDetailBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetProviderCommissionBenefitEntitlementDetailBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ProviderCommissionBenefitEntitlementDetailBffResponse?> Handle(GetProviderCommissionBenefitEntitlementDetailBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.GetProviderCommissionBenefitEntitlementDetailAsync(request.Id, ct) };
}
