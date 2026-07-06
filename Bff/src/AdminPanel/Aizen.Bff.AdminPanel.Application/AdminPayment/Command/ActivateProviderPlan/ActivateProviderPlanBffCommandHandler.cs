using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.ActivateProviderPlan;

[DocumentationInfo("Activate provider plan BFF command handler",
    "Proxies admin activate-provider-plan request to the Payment module. Sets IsActive=true. " +
    "Does not create subscriptions or trigger billing.")]
public sealed class ActivateProviderPlanBffCommandHandler
    : AizenCommandHandler<ActivateProviderPlanBffCommand, PlanMutateBffResult>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public ActivateProviderPlanBffCommandHandler(IAdminPaymentBffRemoteCall remote)
        => _remote = remote;

    public override async Task<PlanMutateBffResult?> Handle(
        ActivateProviderPlanBffCommand request, CancellationToken ct)
    {
        return await _remote.ActivateProviderPlanAsync(request.Id, ct);
    }
}
