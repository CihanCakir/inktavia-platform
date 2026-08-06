using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreateCustomerBenefitBudgetPolicy;

[DocumentationInfo("Create customer-benefit budget policy BFF command handler (BE-P6)",
    "Forwards a new per-plan benefit-budget policy (POST /benefit-budget/policies). CustomerBenefitBudgetPolicyConflict surfaces through the envelope.")]
public sealed class CreateCustomerBenefitBudgetPolicyBffCommandHandler
    : AizenCommandHandler<CreateCustomerBenefitBudgetPolicyBffCommand, CreateCustomerBenefitBudgetPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreateCustomerBenefitBudgetPolicyBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreateCustomerBenefitBudgetPolicyBffResponse?> Handle(CreateCustomerBenefitBudgetPolicyBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreateCustomerBenefitBudgetPolicyAsync(request.Body, ct) };
}
