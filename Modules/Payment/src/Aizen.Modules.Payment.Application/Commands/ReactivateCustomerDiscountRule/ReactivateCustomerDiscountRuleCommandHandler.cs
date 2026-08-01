using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.ReactivateCustomerDiscountRule;

[DocumentationInfo("ReactivateCustomerDiscountRuleCommandHandler",
    "Admin re-activates an Inactive customer-discount rule. Calls Reactivate() which sets IsActive=true and re-derives " +
    "Status from the effective dates. Only Inactive rules can be reactivated (throws CustomerDiscountRuleNotInactive). " +
    "Re-runs the specificity/overlap guard (FindOverlappingActiveRuleAsync) before persisting: reactivation must not " +
    "collide with an existing active rule at the same specificity + priority (throws CustomerDiscountRuleConflict). " +
    "Throws CustomerDiscountRuleNotFound if the rule does not exist.")]
public sealed class ReactivateCustomerDiscountRuleCommandHandler
    : AizenCommandHandler<ReactivateCustomerDiscountRuleCommand, ReactivateCustomerDiscountRuleResult>
{
    private readonly ICustomerDiscountRuleRepository                       _rules;
    private readonly ILogger<ReactivateCustomerDiscountRuleCommandHandler> _logger;

    public ReactivateCustomerDiscountRuleCommandHandler(
        ICustomerDiscountRuleRepository                       rules,
        ILogger<ReactivateCustomerDiscountRuleCommandHandler> logger)
    {
        _rules  = rules;
        _logger = logger;
    }

    public override async Task<ReactivateCustomerDiscountRuleResult?> Handle(
        ReactivateCustomerDiscountRuleCommand request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CustomerDiscountRuleNotFound);

        if (rule.Status != CommissionRuleStatus.Inactive)
            throw new AizenBusinessException((int)PaymentErrorCode.CustomerDiscountRuleNotInactive);

        // Overlap guard: reactivation must not collide with an active rule at the same specificity + priority.
        var conflict = await _rules.FindOverlappingActiveRuleAsync(rule, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.CustomerDiscountRuleConflict,
                $"Reactivation would conflict with an existing active customer discount rule (Id={conflict.Id}, " +
                $"Code={conflict.RuleCode}) at the same specificity and priority with an overlapping effective window.");

        rule.Reactivate();
        _rules.Update(rule);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Customer discount rule reactivated. Id={Id} Code={Code} NewStatus={Status}",
            rule.Id, rule.RuleCode, rule.Status);

        return new ReactivateCustomerDiscountRuleResult(rule.Id, rule.RuleCode, rule.Status.ToString());
    }
}
