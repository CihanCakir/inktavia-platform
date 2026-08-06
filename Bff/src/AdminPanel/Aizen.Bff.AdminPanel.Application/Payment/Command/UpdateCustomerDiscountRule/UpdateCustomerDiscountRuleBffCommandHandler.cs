using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateCustomerDiscountRule;

[DocumentationInfo("Update customer-discount rule BFF command handler (BE-P6)",
    "Forwards a customer-discount rule update (PUT /customer-discounts/rules/{id}). Conflict/Invalid surfaces through the envelope.")]
public sealed class UpdateCustomerDiscountRuleBffCommandHandler
    : AizenCommandHandler<UpdateCustomerDiscountRuleBffCommand, UpdateCustomerDiscountRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public UpdateCustomerDiscountRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<UpdateCustomerDiscountRuleBffResponse?> Handle(UpdateCustomerDiscountRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdateCustomerDiscountRuleAsync(request.Id, request.Body with { Id = request.Id }, ct) };
}
