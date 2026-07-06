using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.DeactivateParticipantPlan;

public sealed class DeactivateParticipantPlanBffCommand : AizenCommand<PlanMutateBffResult>
{
    public long Id { get; init; }
}
