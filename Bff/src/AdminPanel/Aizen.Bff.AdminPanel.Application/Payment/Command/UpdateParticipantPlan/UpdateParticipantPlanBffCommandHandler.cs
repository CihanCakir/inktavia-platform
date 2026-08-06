using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateParticipantPlan;

[DocumentationInfo("Update participant plan BFF command handler",
    "Proxies admin update-participant-plan request to the Payment module.")]
public sealed class UpdateParticipantPlanBffCommandHandler
    : AizenCommandHandler<UpdateParticipantPlanBffCommand, PlanMutateBffResult>
{
    private readonly IPaymentRemoteCall _remote;

    public UpdateParticipantPlanBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<PlanMutateBffResult?> Handle(
        UpdateParticipantPlanBffCommand request, CancellationToken ct)
    {
        var result = await _remote.UpdateParticipantPlanAsync(request.Id,
            new UpdateParticipantPlanBffRequest(
                request.Name, request.Description, request.MonthlyPriceTRY,
                request.AnnualPriceTRY, request.TrialDays, request.BadgeLabel,
                request.ServiceDiscountRate, request.CargoDryDiscountRate,
                request.InkCoinEarnMultiplier, request.SortOrder,
                request.ValidFrom, request.ValidTo, request.FeatureItems), ct);
        return result;
    }
}
