using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreateProviderCommissionBenefitRule;

[DocumentationInfo("Create provider-commission-benefit rule BFF command handler (BE-P7)",
    "Forwards a new benefit rule (POST /commission-benefits/rules). Conflict/Invalid (e.g. both Stackable and Exclusive) surfaces through the envelope.")]
public sealed class CreateProviderCommissionBenefitRuleBffCommandHandler
    : AizenCommandHandler<CreateProviderCommissionBenefitRuleBffCommand, CreateProviderCommissionBenefitRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreateProviderCommissionBenefitRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreateProviderCommissionBenefitRuleBffResponse?> Handle(CreateProviderCommissionBenefitRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreateProviderCommissionBenefitRuleAsync(request.Body, ct) };
}
