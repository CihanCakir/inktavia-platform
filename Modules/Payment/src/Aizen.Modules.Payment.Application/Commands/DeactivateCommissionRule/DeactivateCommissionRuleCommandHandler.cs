using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.DeactivateCommissionRule;

[DocumentationInfo("DeactivateCommissionRuleCommandHandler",
    "Admin explicitly deactivates a commission rule. " +
    "Sets Status=Inactive and IsActive=false via the Deactivate() domain method. " +
    "Throws CommissionRuleNotFound if the rule does not exist.")]
public sealed class DeactivateCommissionRuleCommandHandler
    : AizenCommandHandler<DeactivateCommissionRuleCommand, DeactivateCommissionRuleResult>
{
    private readonly ICommissionRuleRepository                   _rules;
    private readonly ILogger<DeactivateCommissionRuleCommandHandler> _logger;

    public DeactivateCommissionRuleCommandHandler(
        ICommissionRuleRepository                            rules,
        ILogger<DeactivateCommissionRuleCommandHandler>      logger)
    {
        _rules  = rules;
        _logger = logger;
    }

    public override async Task<DeactivateCommissionRuleResult?> Handle(
        DeactivateCommissionRuleCommand request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CommissionRuleNotFound);

        rule.Deactivate();
        _rules.Update(rule);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Commission rule deactivated. Id={Id} RuleCode={Code}",
            rule.Id, rule.RuleCode);

        return new DeactivateCommissionRuleResult(rule.Id, rule.RuleCode);
    }
}
