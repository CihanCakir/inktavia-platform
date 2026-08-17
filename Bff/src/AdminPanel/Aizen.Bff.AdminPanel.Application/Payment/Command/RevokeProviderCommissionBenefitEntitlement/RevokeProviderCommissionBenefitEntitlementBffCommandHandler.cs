using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.RevokeProviderCommissionBenefitEntitlement;

[DocumentationInfo("Revoke provider-commission-benefit entitlement BFF command handler (BE-P7)",
    "Forwards an entitlement revoke (POST /commission-benefits/entitlements/{id}/revoke).")]
public sealed class RevokeProviderCommissionBenefitEntitlementBffCommandHandler
    : AizenCommandHandler<RevokeProviderCommissionBenefitEntitlementBffCommand, RevokeProviderCommissionBenefitEntitlementBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public RevokeProviderCommissionBenefitEntitlementBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<RevokeProviderCommissionBenefitEntitlementBffResponse?> Handle(RevokeProviderCommissionBenefitEntitlementBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.RevokeProviderCommissionBenefitEntitlementAsync(request.Id, ct) };
}
