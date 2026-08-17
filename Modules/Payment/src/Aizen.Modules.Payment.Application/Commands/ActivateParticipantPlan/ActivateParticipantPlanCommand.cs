using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.ActivateParticipantPlan;

public sealed class ActivateParticipantPlanCommand : AizenCommand<ActivateParticipantPlanResult>
{
    public long Id { get; init; }
}

public sealed record ActivateParticipantPlanResult(long Id, string PlanCode);
