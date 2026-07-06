using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.DeactivateProviderPlan;

public sealed class DeactivateProviderPlanBffCommand : AizenCommand<PlanMutateBffResult>
{
    public long Id { get; init; }
}
