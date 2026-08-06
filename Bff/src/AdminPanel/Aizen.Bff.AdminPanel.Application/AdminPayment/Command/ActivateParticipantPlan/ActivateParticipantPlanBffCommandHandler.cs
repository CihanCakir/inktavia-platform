using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.ActivateParticipantPlan;

[DocumentationInfo("Activate participant plan BFF command handler",
    "Proxies admin activate-participant-plan request to the Payment module. Sets IsActive=true. " +
    "Does not create subscriptions or trigger billing.")]
public sealed class ActivateParticipantPlanBffCommandHandler
    : AizenCommandHandler<ActivateParticipantPlanBffCommand, PlanMutateBffResult>
{
    private readonly IPaymentRemoteCall _remote;

    public ActivateParticipantPlanBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<PlanMutateBffResult?> Handle(
        ActivateParticipantPlanBffCommand request, CancellationToken ct)
    {
        return await _remote.ActivateParticipantPlanAsync(request.Id, ct);
    }
}
