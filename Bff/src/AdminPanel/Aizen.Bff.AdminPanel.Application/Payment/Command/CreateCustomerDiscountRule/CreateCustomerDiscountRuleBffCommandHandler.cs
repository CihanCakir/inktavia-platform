using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreateCustomerDiscountRule;

[DocumentationInfo("Create customer-discount rule BFF command handler (BE-P6)",
    "Forwards a new customer-discount rule (POST /customer-discounts/rules). CustomerDiscountRuleConflict/Invalid surfaces through the envelope.")]
public sealed class CreateCustomerDiscountRuleBffCommandHandler
    : AizenCommandHandler<CreateCustomerDiscountRuleBffCommand, CreateCustomerDiscountRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreateCustomerDiscountRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreateCustomerDiscountRuleBffResponse?> Handle(CreateCustomerDiscountRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreateCustomerDiscountRuleAsync(request.Body, ct) };
}
