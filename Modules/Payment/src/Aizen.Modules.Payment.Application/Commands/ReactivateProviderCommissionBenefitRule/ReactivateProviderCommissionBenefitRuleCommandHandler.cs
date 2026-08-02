using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.ReactivateProviderCommissionBenefitRule;

[DocumentationInfo("ReactivateProviderCommissionBenefitRuleCommandHandler",
    "Admin re-activates an Inactive provider commission-benefit rule. Calls Reactivate() which sets IsActive=true and " +
    "re-derives Status from the effective dates. Only Inactive rules can be reactivated (throws " +
    "ProviderCommissionBenefitRuleNotInactive). Re-runs the specificity/overlap guard (FindOverlappingActiveRuleAsync) " +
    "before persisting: reactivation must not collide with an existing active rule at the same specificity + priority " +
    "(throws ProviderCommissionBenefitRuleConflict). Throws ProviderCommissionBenefitRuleNotFound if the rule is missing.")]
public sealed class ReactivateProviderCommissionBenefitRuleCommandHandler
    : AizenCommandHandler<ReactivateProviderCommissionBenefitRuleCommand, ReactivateProviderCommissionBenefitRuleResult>
{
    private readonly IProviderCommissionBenefitRuleRepository                       _rules;
    private readonly ILogger<ReactivateProviderCommissionBenefitRuleCommandHandler> _logger;

    public ReactivateProviderCommissionBenefitRuleCommandHandler(
        IProviderCommissionBenefitRuleRepository                       rules,
        ILogger<ReactivateProviderCommissionBenefitRuleCommandHandler> logger)
    {
        _rules  = rules;
        _logger = logger;
    }

    public override async Task<ReactivateProviderCommissionBenefitRuleResult?> Handle(
        ReactivateProviderCommissionBenefitRuleCommand request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitRuleNotFound);

        if (rule.Status != CommissionRuleStatus.Inactive)
            throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitRuleNotInactive);

        // Overlap guard: reactivation must not collide with an active rule at the same specificity + priority.
        var conflict = await _rules.FindOverlappingActiveRuleAsync(rule, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.ProviderCommissionBenefitRuleConflict,
                $"Reactivation would conflict with an existing active provider commission benefit rule (Id={conflict.Id}, " +
                $"Code={conflict.RuleCode}) at the same specificity and priority with an overlapping effective window.");

        rule.Reactivate();
        _rules.Update(rule);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Provider commission benefit rule reactivated. Id={Id} Code={Code} NewStatus={Status}",
            rule.Id, rule.RuleCode, rule.Status);

        return new ReactivateProviderCommissionBenefitRuleResult(rule.Id, rule.RuleCode, rule.Status.ToString());
    }
}
