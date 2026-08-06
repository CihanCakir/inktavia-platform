using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ActivateProviderPlan;

public sealed class ActivateProviderPlanBffCommand : AizenCommand<PlanMutateBffResult>
{
    public long Id { get; init; }
}
