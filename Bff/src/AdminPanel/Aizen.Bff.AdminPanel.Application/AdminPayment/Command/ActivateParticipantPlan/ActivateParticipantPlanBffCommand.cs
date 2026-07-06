using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.ActivateParticipantPlan;

public sealed class ActivateParticipantPlanBffCommand : AizenCommand<PlanMutateBffResult>
{
    public long Id { get; init; }
}
