using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ActivateProviderPlan;

[DocumentationInfo("Activate provider plan BFF command handler",
    "Proxies admin activate-provider-plan request to the Payment module. Sets IsActive=true. " +
    "Does not create subscriptions or trigger billing.")]
public sealed class ActivateProviderPlanBffCommandHandler
    : AizenCommandHandler<ActivateProviderPlanBffCommand, PlanMutateBffResult>
{
    private readonly IPaymentRemoteCall _remote;

    public ActivateProviderPlanBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<PlanMutateBffResult?> Handle(
        ActivateProviderPlanBffCommand request, CancellationToken ct)
    {
        return await _remote.ActivateProviderPlanAsync(request.Id, ct);
    }
}
