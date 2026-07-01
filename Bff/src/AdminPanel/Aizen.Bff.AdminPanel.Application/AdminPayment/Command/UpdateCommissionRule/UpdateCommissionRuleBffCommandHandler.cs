using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.UpdateCommissionRule;

[DocumentationInfo("Update commission rule BFF command handler",
    "Forwards a commission rule update to the Payment module via PUT. " +
    "Only mutable fields (CommissionRate, EffectiveFrom/To, Priority, Notes) are sent — " +
    "RuleType, CategoryCode, ProviderPlanId, ProviderProfileId are immutable after creation. " +
    "Returns the updated entity Id and RuleCode for frontend invalidation.")]
public sealed class UpdateCommissionRuleBffCommandHandler
    : AizenCommandHandler<UpdateCommissionRuleBffCommand, UpdateCommissionRuleBffCommandResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;

    public UpdateCommissionRuleBffCommandHandler(IAdminPaymentBffRemoteCall payment)
        => _payment = payment;

    public override async Task<UpdateCommissionRuleBffCommandResponse?> Handle(
        UpdateCommissionRuleBffCommand request, CancellationToken ct)
    {
        var result = await _payment.UpdateCommissionRuleAsync(request.Id, request.Body, ct);
        return new UpdateCommissionRuleBffCommandResponse { Result = result };
    }
}
