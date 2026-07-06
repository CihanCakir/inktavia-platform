using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.ActivateProviderPlan;

public sealed class ActivateProviderPlanBffCommand : AizenCommand<PlanMutateBffResult>
{
    public long Id { get; init; }
}
