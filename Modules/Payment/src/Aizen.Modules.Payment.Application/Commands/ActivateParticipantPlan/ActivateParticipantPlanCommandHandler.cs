using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.ActivateParticipantPlan;

[DocumentationInfo("ActivateParticipantPlanCommand handler",
    "Sets IsActive=true on a participant subscription plan via the Activate() domain method. " +
    "Does NOT create subscriptions or trigger billing. " +
    "Throws ParticipantPlanNotFound if the plan does not exist.")]
public sealed class ActivateParticipantPlanCommandHandler
    : AizenCommandHandler<ActivateParticipantPlanCommand, ActivateParticipantPlanResult>
{
    private readonly IParticipantPlanRepository                        _plans;
    private readonly ILogger<ActivateParticipantPlanCommandHandler>    _logger;

    public ActivateParticipantPlanCommandHandler(
        IParticipantPlanRepository                      plans,
        ILogger<ActivateParticipantPlanCommandHandler>  logger)
    {
        _plans  = plans;
        _logger = logger;
    }

    public override async Task<ActivateParticipantPlanResult?> Handle(
        ActivateParticipantPlanCommand request, CancellationToken ct)
    {
        var plan = await _plans.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ParticipantPlanNotFound);

        plan.Activate();
        _plans.Update(plan);

        _logger.LogInformation(
            "Participant plan activated. Id={Id} PlanCode={Code}", plan.Id, plan.PlanCode);

        return new ActivateParticipantPlanResult(plan.Id, plan.PlanCode);
    }
}
