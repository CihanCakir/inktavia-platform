using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.ActivateProviderPlan;

public sealed class ActivateProviderPlanCommand : AizenCommand<ActivateProviderPlanResult>
{
    public long Id { get; init; }
}

public sealed record ActivateProviderPlanResult(long Id, string PlanCode);
