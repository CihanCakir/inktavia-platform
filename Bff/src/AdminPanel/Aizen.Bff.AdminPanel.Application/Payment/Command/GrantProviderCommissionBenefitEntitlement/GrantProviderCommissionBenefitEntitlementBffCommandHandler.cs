using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.GrantProviderCommissionBenefitEntitlement;

[DocumentationInfo("Grant provider-commission-benefit entitlement BFF command handler (BE-P7)",
    "Forwards an entitlement grant (POST /commission-benefits/entitlements).")]
public sealed class GrantProviderCommissionBenefitEntitlementBffCommandHandler
    : AizenCommandHandler<GrantProviderCommissionBenefitEntitlementBffCommand, GrantProviderCommissionBenefitEntitlementBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GrantProviderCommissionBenefitEntitlementBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GrantProviderCommissionBenefitEntitlementBffResponse?> Handle(GrantProviderCommissionBenefitEntitlementBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.GrantProviderCommissionBenefitEntitlementAsync(request.Body, ct) };
}
