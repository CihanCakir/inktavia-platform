using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.ReactivatePlatformFeeRule;

[DocumentationInfo("ReactivatePlatformFeeRuleCommandHandler",
    "Admin re-activates an Inactive platform fee rule. Calls Reactivate() which sets IsActive=true and re-derives " +
    "Status from the effective dates. Only Inactive rules can be reactivated (throws PlatformFeeRuleNotInactive). " +
    "Re-runs the §7 fail-loud conflict guard (FindOverlappingActiveRuleAsync) before persisting: reactivation must " +
    "not collide with another active rule at the same scope-key + priority + overlapping window " +
    "(throws PlatformFeeRuleConflict). Throws PlatformFeeRuleNotFound if the rule does not exist.")]
public sealed class ReactivatePlatformFeeRuleCommandHandler
    : AizenCommandHandler<ReactivatePlatformFeeRuleCommand, ReactivatePlatformFeeRuleResult>
{
    private readonly IPlatformFeeRuleRepository                        _rules;
    private readonly ILogger<ReactivatePlatformFeeRuleCommandHandler>  _logger;

    public ReactivatePlatformFeeRuleCommandHandler(
        IPlatformFeeRuleRepository                        rules,
        ILogger<ReactivatePlatformFeeRuleCommandHandler>  logger)
    {
        _rules  = rules;
        _logger = logger;
    }

    public override async Task<ReactivatePlatformFeeRuleResult?> Handle(
        ReactivatePlatformFeeRuleCommand request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PlatformFeeRuleNotFound);

        if (rule.Status != CommissionRuleStatus.Inactive)
            throw new AizenBusinessException((int)PaymentErrorCode.PlatformFeeRuleNotInactive);

        // §7 fail-loud conflict guard: reactivation must not collide with an existing active rule at the same
        // scope-key + priority + overlapping window.
        var conflict = await _rules.FindOverlappingActiveRuleAsync(rule, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.PlatformFeeRuleConflict,
                $"Reactivation would conflict with an existing active platform fee rule (Id={conflict.Id}, " +
                $"RuleCode={conflict.RuleCode}) with the same scope, priority, and an overlapping effective window.");

        rule.Reactivate();
        _rules.Update(rule);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Platform fee rule reactivated. Id={Id} RuleCode={Code} NewStatus={Status}",
            rule.Id, rule.RuleCode, rule.Status);

        return new ReactivatePlatformFeeRuleResult(rule.Id, rule.RuleCode, rule.Status.ToString());
    }
}
