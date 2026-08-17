using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Domain.Entities.Plan;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CreateProviderPlan;

[DocumentationInfo("CreateProviderPlanCommandHandler",
    "Admin creates a new provider subscription plan. Enforces PlanCode uniqueness. " +
    "SaveChanges is handled by the AizenCommandHandlerDecorator.")]
public sealed class CreateProviderPlanCommandHandler
    : AizenCommandHandler<CreateProviderPlanCommand, CreateProviderPlanResult>
{
    private readonly IProviderPlanRepository                  _plans;
    private readonly ILogger<CreateProviderPlanCommandHandler> _logger;

    public CreateProviderPlanCommandHandler(
        IProviderPlanRepository                        plans,
        ILogger<CreateProviderPlanCommandHandler>      logger)
    {
        _plans  = plans;
        _logger = logger;
    }

    public override async Task<CreateProviderPlanResult?> Handle(
        CreateProviderPlanCommand request, CancellationToken ct)
    {
        if (await _plans.ExistsByCodeAsync(request.PlanCode.ToUpperInvariant(), ct))
            throw new AizenBusinessException(
                $"A provider plan with code '{request.PlanCode}' already exists.");

        var plan = ProviderPlanEntity.Create(
            request.PlanCode.ToUpperInvariant(), request.Name, request.Description,
            request.MonthlyPriceTRY, request.AnnualPriceTRY, request.TrialDays, request.BadgeLabel,
            request.MaxActiveOffers, request.HasPriorityBoost, request.HasFullAnalytics, request.SortOrder,
            request.ValidFrom, request.ValidTo, request.FeatureItems);

        await _plans.AddAsync(plan, ct);

        _logger.LogInformation(
            "ProviderPlan created. PlanCode={Code} Price={Price}",
            plan.PlanCode, plan.MonthlyPriceTRY);

        return new CreateProviderPlanResult(plan.Id, plan.PlanCode);
    }
}
