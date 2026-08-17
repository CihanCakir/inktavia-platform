using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.ReactivateCommissionRule;

[DocumentationInfo("ReactivateCommissionRuleCommandHandler",
    "Admin re-activates an Inactive commission rule. " +
    "Calls Reactivate() domain method which sets IsActive=true and re-derives Status from effective dates. " +
    "Only Inactive rules can be reactivated; Expired rules must be updated (EffectiveTo extended) first. " +
    "Throws CommissionRuleNotFound if rule does not exist. " +
    "Throws CommissionRuleNotInactive if the rule is not currently Inactive.")]
public sealed class ReactivateCommissionRuleCommandHandler
    : AizenCommandHandler<ReactivateCommissionRuleCommand, ReactivateCommissionRuleResult>
{
    private readonly ICommissionRuleRepository                      _rules;
    private readonly ILogger<ReactivateCommissionRuleCommandHandler> _logger;

    public ReactivateCommissionRuleCommandHandler(
        ICommissionRuleRepository                             rules,
        ILogger<ReactivateCommissionRuleCommandHandler>       logger)
    {
        _rules  = rules;
        _logger = logger;
    }

    public override async Task<ReactivateCommissionRuleResult?> Handle(
        ReactivateCommissionRuleCommand request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CommissionRuleNotFound);

        if (rule.Status != CommissionRuleStatus.Inactive)
            throw new AizenBusinessException((int)PaymentErrorCode.CommissionRuleNotInactive);

        rule.Reactivate();
        _rules.Update(rule);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Commission rule reactivated. Id={Id} RuleCode={Code} NewStatus={Status}",
            rule.Id, rule.RuleCode, rule.Status);

        return new ReactivateCommissionRuleResult(rule.Id, rule.RuleCode, rule.Status.ToString());
    }
}
