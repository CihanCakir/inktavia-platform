using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.DeactivateProviderPlan;

[DocumentationInfo("Deactivate provider plan BFF command handler",
    "Proxies admin deactivate-provider-plan request to the Payment module. Sets IsActive=false. " +
    "Does not cancel existing subscriptions or affect billing.")]
public sealed class DeactivateProviderPlanBffCommandHandler
    : AizenCommandHandler<DeactivateProviderPlanBffCommand, PlanMutateBffResult>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public DeactivateProviderPlanBffCommandHandler(IAdminPaymentBffRemoteCall remote)
        => _remote = remote;

    public override async Task<PlanMutateBffResult?> Handle(
        DeactivateProviderPlanBffCommand request, CancellationToken ct)
    {
        return await _remote.DeactivateProviderPlanAsync(request.Id, ct);
    }
}
