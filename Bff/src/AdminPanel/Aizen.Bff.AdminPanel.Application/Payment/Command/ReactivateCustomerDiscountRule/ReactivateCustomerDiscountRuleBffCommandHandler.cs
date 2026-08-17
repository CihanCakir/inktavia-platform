using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReactivateCustomerDiscountRule;

[DocumentationInfo("Reactivate customer-discount rule BFF command handler (BE-P6)",
    "Forwards a reactivate (POST /customer-discounts/rules/{id}/reactivate). The module re-runs the specificity/overlap " +
    "guard, so CustomerDiscountRuleConflict may surface through the envelope.")]
public sealed class ReactivateCustomerDiscountRuleBffCommandHandler
    : AizenCommandHandler<ReactivateCustomerDiscountRuleBffCommand, ReactivateCustomerDiscountRuleBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ReactivateCustomerDiscountRuleBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ReactivateCustomerDiscountRuleBffResponse?> Handle(ReactivateCustomerDiscountRuleBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.ReactivateCustomerDiscountRuleAsync(request.Id, ct) };
}
