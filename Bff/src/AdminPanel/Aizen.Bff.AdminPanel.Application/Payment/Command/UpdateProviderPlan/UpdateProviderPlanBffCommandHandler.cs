using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateProviderPlan;

[DocumentationInfo("Update provider plan BFF command handler",
    "Proxies admin update-provider-plan request to the Payment module.")]
public sealed class UpdateProviderPlanBffCommandHandler
    : AizenCommandHandler<UpdateProviderPlanBffCommand, PlanMutateBffResult>
{
    private readonly IPaymentRemoteCall _remote;

    public UpdateProviderPlanBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<PlanMutateBffResult?> Handle(
        UpdateProviderPlanBffCommand request, CancellationToken ct)
    {
        var result = await _remote.UpdateProviderPlanAsync(request.Id,
            new UpdateProviderPlanBffRequest(
                request.Name, request.Description, request.MonthlyPriceTRY,
                request.AnnualPriceTRY, request.TrialDays, request.BadgeLabel,
                request.MaxActiveOffers, request.HasPriorityBoost,
                request.HasFullAnalytics, request.SortOrder,
                request.ValidFrom, request.ValidTo, request.FeatureItems), ct);
        return result;
    }
}
