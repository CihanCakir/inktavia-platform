using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.DeactivateParticipantPlan;

public sealed class DeactivateParticipantPlanCommand : AizenCommand<DeactivateParticipantPlanResult>
{
    public long Id { get; init; }
}

public sealed record DeactivateParticipantPlanResult(long Id, string PlanCode);
