using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivateProviderCommissionBenefitRule;

[DocumentationInfo("Deactivate provider-commission-benefit rule BFF command handler (BE-P7)",
    "Forwards a deactivate (POST /commission-benefits/rules/{id}/deactivate).")]
public sealed class DeactivateProviderCommissionBenefitRuleBffCommandHandler
    : AizenCommandHandler<DeactivateProviderCommissionBenefitRuleBffCommand, DeactivateProviderCommissionBenefitRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public DeactivateProviderCommissionBenefitRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<DeactivateProviderCommissionBenefitRuleBffResponse?> Handle(DeactivateProviderCommissionBenefitRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivateProviderCommissionBenefitRuleAsync(request.Id, ct) };
}
