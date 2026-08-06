using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivateProviderPlan;

[DocumentationInfo("Deactivate provider plan BFF command handler",
    "Proxies admin deactivate-provider-plan request to the Payment module. Sets IsActive=false. " +
    "Does not cancel existing subscriptions or affect billing.")]
public sealed class DeactivateProviderPlanBffCommandHandler
    : AizenCommandHandler<DeactivateProviderPlanBffCommand, PlanMutateBffResult>
{
    private readonly IPaymentRemoteCall _remote;

    public DeactivateProviderPlanBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<PlanMutateBffResult?> Handle(
        DeactivateProviderPlanBffCommand request, CancellationToken ct)
    {
        return await _remote.DeactivateProviderPlanAsync(request.Id, ct);
    }
}
