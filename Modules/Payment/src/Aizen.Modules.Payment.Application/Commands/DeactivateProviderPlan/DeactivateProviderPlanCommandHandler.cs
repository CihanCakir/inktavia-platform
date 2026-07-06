using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.DeactivateProviderPlan;

[DocumentationInfo("DeactivateProviderPlanCommand handler",
    "Sets IsActive=false on a provider subscription plan via the Deactivate() domain method. " +
    "Does NOT cancel existing subscriptions or affect billing. " +
    "Throws ProviderPlanNotFound if the plan does not exist.")]
public sealed class DeactivateProviderPlanCommandHandler
    : AizenCommandHandler<DeactivateProviderPlanCommand, DeactivateProviderPlanResult>
{
    private readonly IProviderPlanRepository                         _plans;
    private readonly ILogger<DeactivateProviderPlanCommandHandler>   _logger;

    public DeactivateProviderPlanCommandHandler(
        IProviderPlanRepository                       plans,
        ILogger<DeactivateProviderPlanCommandHandler> logger)
    {
        _plans  = plans;
        _logger = logger;
    }

    public override async Task<DeactivateProviderPlanResult?> Handle(
        DeactivateProviderPlanCommand request, CancellationToken ct)
    {
        var plan = await _plans.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderPlanNotFound);

        plan.Deactivate();
        _plans.Update(plan);

        _logger.LogInformation(
            "Provider plan deactivated. Id={Id} PlanCode={Code}", plan.Id, plan.PlanCode);

        return new DeactivateProviderPlanResult(plan.Id, plan.PlanCode);
    }
}
