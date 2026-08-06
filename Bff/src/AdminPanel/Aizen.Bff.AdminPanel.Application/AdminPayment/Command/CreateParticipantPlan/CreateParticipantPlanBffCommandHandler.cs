using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CreateParticipantPlan;

[DocumentationInfo("Create participant plan BFF command handler",
    "Proxies admin create-participant-plan request to the Payment module.")]
public sealed class CreateParticipantPlanBffCommandHandler
    : AizenCommandHandler<CreateParticipantPlanBffCommand, PlanMutateBffResult>
{
    private readonly IPaymentRemoteCall _remote;

    public CreateParticipantPlanBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<PlanMutateBffResult?> Handle(
        CreateParticipantPlanBffCommand request, CancellationToken ct)
    {
        var result = await _remote.CreateParticipantPlanAsync(
            new CreateParticipantPlanBffRequest(
                request.PlanCode, request.Name, request.Description,
                request.MonthlyPriceTRY, request.AnnualPriceTRY, request.TrialDays, request.BadgeLabel,
                request.ServiceDiscountRate, request.CargoDryDiscountRate,
                request.InkCoinEarnMultiplier, request.SortOrder,
                request.ValidFrom, request.ValidTo, request.FeatureItems), ct);
        return result;
    }
}
