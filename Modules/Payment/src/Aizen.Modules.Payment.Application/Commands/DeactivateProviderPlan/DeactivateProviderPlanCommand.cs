using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.DeactivateProviderPlan;

public sealed class DeactivateProviderPlanCommand : AizenCommand<DeactivateProviderPlanResult>
{
    public long Id { get; init; }
}

public sealed record DeactivateProviderPlanResult(long Id, string PlanCode);
