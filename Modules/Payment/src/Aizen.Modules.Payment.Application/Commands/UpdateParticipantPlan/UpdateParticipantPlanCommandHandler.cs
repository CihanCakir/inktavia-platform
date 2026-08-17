using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.UpdateParticipantPlan;

[DocumentationInfo("UpdateParticipantPlanCommandHandler",
    "Admin updates an existing participant subscription plan. PlanCode is immutable. " +
    "SaveChanges is handled by the AizenCommandHandlerDecorator.")]
public sealed class UpdateParticipantPlanCommandHandler
    : AizenCommandHandler<UpdateParticipantPlanCommand, UpdateParticipantPlanResult>
{
    private readonly IParticipantPlanRepository                   _plans;
    private readonly ILogger<UpdateParticipantPlanCommandHandler>  _logger;

    public UpdateParticipantPlanCommandHandler(
        IParticipantPlanRepository                        plans,
        ILogger<UpdateParticipantPlanCommandHandler>      logger)
    {
        _plans  = plans;
        _logger = logger;
    }

    public override async Task<UpdateParticipantPlanResult?> Handle(
        UpdateParticipantPlanCommand request, CancellationToken ct)
    {
        var plan = await _plans.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException($"Participant plan with id '{request.Id}' not found.");

        plan.Update(
            request.Name, request.Description,
            request.MonthlyPriceTRY, request.AnnualPriceTRY, request.TrialDays, request.BadgeLabel,
            request.ServiceDiscountRate, request.CargoDryDiscountRate,
            request.InkCoinEarnMultiplier, request.SortOrder,
            request.ValidFrom, request.ValidTo, request.FeatureItems);

        _plans.Update(plan);

        _logger.LogInformation(
            "ParticipantPlan updated. Id={Id} PlanCode={Code}",
            plan.Id, plan.PlanCode);

        return new UpdateParticipantPlanResult(plan.Id, plan.PlanCode);
    }
}
