using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.ActivateProviderPlan;

[DocumentationInfo("ActivateProviderPlanCommand handler",
    "Sets IsActive=true on a provider subscription plan via the Activate() domain method. " +
    "Does NOT create subscriptions or trigger billing. " +
    "Throws ProviderPlanNotFound if the plan does not exist.")]
public sealed class ActivateProviderPlanCommandHandler
    : AizenCommandHandler<ActivateProviderPlanCommand, ActivateProviderPlanResult>
{
    private readonly IProviderPlanRepository                       _plans;
    private readonly ILogger<ActivateProviderPlanCommandHandler>   _logger;

    public ActivateProviderPlanCommandHandler(
        IProviderPlanRepository                     plans,
        ILogger<ActivateProviderPlanCommandHandler> logger)
    {
        _plans  = plans;
        _logger = logger;
    }

    public override async Task<ActivateProviderPlanResult?> Handle(
        ActivateProviderPlanCommand request, CancellationToken ct)
    {
        var plan = await _plans.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderPlanNotFound);

        plan.Activate();
        _plans.Update(plan);

        _logger.LogInformation(
            "Provider plan activated. Id={Id} PlanCode={Code}", plan.Id, plan.PlanCode);

        return new ActivateProviderPlanResult(plan.Id, plan.PlanCode);
    }
}
