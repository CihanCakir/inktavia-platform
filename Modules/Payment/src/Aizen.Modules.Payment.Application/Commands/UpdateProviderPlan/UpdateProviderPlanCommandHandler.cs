using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.UpdateProviderPlan;

[DocumentationInfo("UpdateProviderPlanCommandHandler",
    "Admin updates an existing provider subscription plan. PlanCode is immutable. " +
    "SaveChanges is handled by the AizenCommandHandlerDecorator.")]
public sealed class UpdateProviderPlanCommandHandler
    : AizenCommandHandler<UpdateProviderPlanCommand, UpdateProviderPlanResult>
{
    private readonly IProviderPlanRepository                  _plans;
    private readonly ILogger<UpdateProviderPlanCommandHandler> _logger;

    public UpdateProviderPlanCommandHandler(
        IProviderPlanRepository                        plans,
        ILogger<UpdateProviderPlanCommandHandler>      logger)
    {
        _plans  = plans;
        _logger = logger;
    }

    public override async Task<UpdateProviderPlanResult?> Handle(
        UpdateProviderPlanCommand request, CancellationToken ct)
    {
        var plan = await _plans.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException($"Provider plan with id '{request.Id}' not found.");

        plan.Update(
            request.Name, request.Description,
            request.MonthlyPriceTRY, request.AnnualPriceTRY, request.TrialDays, request.BadgeLabel,
            request.MaxActiveOffers, request.HasPriorityBoost, request.HasFullAnalytics, request.SortOrder,
            request.ValidFrom, request.ValidTo, request.FeatureItems);

        _plans.Update(plan);

        _logger.LogInformation(
            "ProviderPlan updated. Id={Id} PlanCode={Code}",
            plan.Id, plan.PlanCode);

        return new UpdateProviderPlanResult(plan.Id, plan.PlanCode);
    }
}
