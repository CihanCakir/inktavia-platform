using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivateParticipantPlan;

public sealed class DeactivateParticipantPlanBffCommand : AizenCommand<PlanMutateBffResult>
{
    public long Id { get; init; }
}
