using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ActivateParticipantPlan;

public sealed class ActivateParticipantPlanBffCommand : AizenCommand<PlanMutateBffResult>
{
    public long Id { get; init; }
}
