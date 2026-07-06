using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.DeactivateParticipantPlan;

[DocumentationInfo("Deactivate participant plan BFF command handler",
    "Proxies admin deactivate-participant-plan request to the Payment module. Sets IsActive=false. " +
    "Does not cancel existing subscriptions or affect billing.")]
public sealed class DeactivateParticipantPlanBffCommandHandler
    : AizenCommandHandler<DeactivateParticipantPlanBffCommand, PlanMutateBffResult>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public DeactivateParticipantPlanBffCommandHandler(IAdminPaymentBffRemoteCall remote)
        => _remote = remote;

    public override async Task<PlanMutateBffResult?> Handle(
        DeactivateParticipantPlanBffCommand request, CancellationToken ct)
    {
        return await _remote.DeactivateParticipantPlanAsync(request.Id, ct);
    }
}
