using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CreateParticipantPlan;

[DocumentationInfo("CreateParticipantPlanCommandHandler",
    "Admin creates a new participant subscription plan. Enforces PlanCode uniqueness. " +
    "SaveChanges is handled by the AizenCommandHandlerDecorator.")]
public sealed class CreateParticipantPlanCommandHandler
    : AizenCommandHandler<CreateParticipantPlanCommand, CreateParticipantPlanResult>
{
    private readonly IParticipantPlanRepository                   _plans;
    private readonly ILogger<CreateParticipantPlanCommandHandler>  _logger;

    public CreateParticipantPlanCommandHandler(
        IParticipantPlanRepository                        plans,
        ILogger<CreateParticipantPlanCommandHandler>      logger)
    {
        _plans  = plans;
        _logger = logger;
    }

    public override async Task<CreateParticipantPlanResult?> Handle(
        CreateParticipantPlanCommand request, CancellationToken ct)
    {
        if (await _plans.ExistsByCodeAsync(request.PlanCode.ToUpperInvariant(), ct))
            throw new AizenBusinessException(
                $"A participant plan with code '{request.PlanCode}' already exists.");

        var plan = ParticipantPlanEntity.Create(
            request.PlanCode.ToUpperInvariant(), request.Name, request.Description,
            request.MonthlyPriceTRY, request.AnnualPriceTRY, request.TrialDays, request.BadgeLabel,
            request.ServiceDiscountRate, request.CargoDryDiscountRate,
            request.InkCoinEarnMultiplier, request.SortOrder,
            request.ValidFrom, request.ValidTo, request.FeatureItems);

        await _plans.AddAsync(plan, ct);

        _logger.LogInformation(
            "ParticipantPlan created. PlanCode={Code} Price={Price}",
            plan.PlanCode, plan.MonthlyPriceTRY);

        return new CreateParticipantPlanResult(plan.Id, plan.PlanCode);
    }
}
