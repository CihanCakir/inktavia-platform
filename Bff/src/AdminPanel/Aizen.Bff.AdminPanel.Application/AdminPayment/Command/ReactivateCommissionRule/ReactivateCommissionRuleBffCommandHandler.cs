using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.ReactivateCommissionRule;

[DocumentationInfo("Reactivate commission rule BFF command handler",
    "Re-activates an Inactive commission rule via POST to the Payment module. " +
    "The Payment module derives the new status from effective dates (Active, Scheduled, or Expired). " +
    "Returns the entity Id and RuleCode for cache invalidation on the frontend.")]
public sealed class ReactivateCommissionRuleBffCommandHandler
    : AizenCommandHandler<ReactivateCommissionRuleBffCommand, ReactivateCommissionRuleBffCommandResponse>
{
    private readonly IPaymentRemoteCall _payment;

    public ReactivateCommissionRuleBffCommandHandler(IPaymentRemoteCall payment)
        => _payment = payment;

    public override async Task<ReactivateCommissionRuleBffCommandResponse?> Handle(
        ReactivateCommissionRuleBffCommand request, CancellationToken ct)
    {
        var result = await _payment.ReactivateCommissionRuleAsync(request.Id, ct);
        return new ReactivateCommissionRuleBffCommandResponse { Result = result };
    }
}
