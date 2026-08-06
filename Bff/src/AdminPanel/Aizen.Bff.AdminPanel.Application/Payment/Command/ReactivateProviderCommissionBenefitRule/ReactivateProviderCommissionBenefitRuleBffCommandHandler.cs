using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReactivateProviderCommissionBenefitRule;

[DocumentationInfo("Reactivate provider-commission-benefit rule BFF command handler (BE-P7)",
    "Forwards a reactivate (POST /commission-benefits/rules/{id}/reactivate). Overlap re-check → 5070 conflict via the envelope.")]
public sealed class ReactivateProviderCommissionBenefitRuleBffCommandHandler
    : AizenCommandHandler<ReactivateProviderCommissionBenefitRuleBffCommand, ReactivateProviderCommissionBenefitRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ReactivateProviderCommissionBenefitRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ReactivateProviderCommissionBenefitRuleBffResponse?> Handle(ReactivateProviderCommissionBenefitRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.ReactivateProviderCommissionBenefitRuleAsync(request.Id, ct) };
}
