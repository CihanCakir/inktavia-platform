using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.DeactivateCommissionRule;

[DocumentationInfo("Deactivate commission rule BFF command handler",
    "Soft-deactivates a commission rule via DELETE to the Payment module. " +
    "The Payment module sets Status = Inactive and IsActive = false; the record is NOT deleted. " +
    "Returns the deactivated entity Id and RuleCode for cache invalidation on the frontend.")]
public sealed class DeactivateCommissionRuleBffCommandHandler
    : AizenCommandHandler<DeactivateCommissionRuleBffCommand, DeactivateCommissionRuleBffCommandResponse>
{
    private readonly IPaymentRemoteCall _payment;

    public DeactivateCommissionRuleBffCommandHandler(IPaymentRemoteCall payment)
        => _payment = payment;

    public override async Task<DeactivateCommissionRuleBffCommandResponse?> Handle(
        DeactivateCommissionRuleBffCommand request, CancellationToken ct)
    {
        var result = await _payment.DeactivateCommissionRuleAsync(request.Id, ct);
        return new DeactivateCommissionRuleBffCommandResponse { Result = result };
    }
}
