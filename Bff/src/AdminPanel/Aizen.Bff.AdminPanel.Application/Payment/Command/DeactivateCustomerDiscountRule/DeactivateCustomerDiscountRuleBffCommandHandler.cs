using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivateCustomerDiscountRule;

[DocumentationInfo("Deactivate customer-discount rule BFF command handler (BE-P6)",
    "Forwards a deactivate (POST /customer-discounts/rules/{id}/deactivate).")]
public sealed class DeactivateCustomerDiscountRuleBffCommandHandler
    : AizenCommandHandler<DeactivateCustomerDiscountRuleBffCommand, DeactivateCustomerDiscountRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public DeactivateCustomerDiscountRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<DeactivateCustomerDiscountRuleBffResponse?> Handle(DeactivateCustomerDiscountRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivateCustomerDiscountRuleAsync(request.Id, ct) };
}
