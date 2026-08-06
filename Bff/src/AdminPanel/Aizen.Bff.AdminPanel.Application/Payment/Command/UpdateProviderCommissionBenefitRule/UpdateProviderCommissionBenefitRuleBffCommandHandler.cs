using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateProviderCommissionBenefitRule;

[DocumentationInfo("Update provider-commission-benefit rule BFF command handler (BE-P7)",
    "Forwards a benefit rule update (PUT /commission-benefits/rules/{id}).")]
public sealed class UpdateProviderCommissionBenefitRuleBffCommandHandler
    : AizenCommandHandler<UpdateProviderCommissionBenefitRuleBffCommand, UpdateProviderCommissionBenefitRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public UpdateProviderCommissionBenefitRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<UpdateProviderCommissionBenefitRuleBffResponse?> Handle(UpdateProviderCommissionBenefitRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdateProviderCommissionBenefitRuleAsync(request.Id, request.Body with { Id = request.Id }, ct) };
}
