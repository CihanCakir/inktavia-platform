using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.DeactivateParticipantPlan;

[DocumentationInfo("DeactivateParticipantPlanCommand handler",
    "Sets IsActive=false on a participant subscription plan via the Deactivate() domain method. " +
    "Does NOT cancel existing subscriptions or affect billing. " +
    "Throws ParticipantPlanNotFound if the plan does not exist.")]
public sealed class DeactivateParticipantPlanCommandHandler
    : AizenCommandHandler<DeactivateParticipantPlanCommand, DeactivateParticipantPlanResult>
{
    private readonly IParticipantPlanRepository                          _plans;
    private readonly ILogger<DeactivateParticipantPlanCommandHandler>    _logger;

    public DeactivateParticipantPlanCommandHandler(
        IParticipantPlanRepository                        plans,
        ILogger<DeactivateParticipantPlanCommandHandler>  logger)
    {
        _plans  = plans;
        _logger = logger;
    }

    public override async Task<DeactivateParticipantPlanResult?> Handle(
        DeactivateParticipantPlanCommand request, CancellationToken ct)
    {
        var plan = await _plans.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ParticipantPlanNotFound);

        plan.Deactivate();
        _plans.Update(plan);

        _logger.LogInformation(
            "Participant plan deactivated. Id={Id} PlanCode={Code}", plan.Id, plan.PlanCode);

        return new DeactivateParticipantPlanResult(plan.Id, plan.PlanCode);
    }
}
