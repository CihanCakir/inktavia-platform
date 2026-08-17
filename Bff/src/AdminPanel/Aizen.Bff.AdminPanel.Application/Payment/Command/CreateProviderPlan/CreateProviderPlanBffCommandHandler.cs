using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreateProviderPlan;

[DocumentationInfo("Create provider plan BFF command handler",
    "Proxies admin create-provider-plan request to the Payment module.")]
public sealed class CreateProviderPlanBffCommandHandler
    : AizenCommandHandler<CreateProviderPlanBffCommand, PlanMutateBffResult>
{
    private readonly IPaymentRemoteCall _remote;

    public CreateProviderPlanBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<PlanMutateBffResult?> Handle(
        CreateProviderPlanBffCommand request, CancellationToken ct)
    {
        var result = await _remote.CreateProviderPlanAsync(
            new CreateProviderPlanBffRequest(
                request.PlanCode, request.Name, request.Description,
                request.MonthlyPriceTRY, request.AnnualPriceTRY, request.TrialDays, request.BadgeLabel,
                request.MaxActiveOffers, request.HasPriorityBoost, request.HasFullAnalytics, request.SortOrder,
                request.ValidFrom, request.ValidTo, request.FeatureItems), ct);
        return result;
    }
}
