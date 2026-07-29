using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.DeactivateCustomerDiscountRule;

[DocumentationInfo("DeactivateCustomerDiscountRuleCommandHandler",
    "Admin deactivates a customer discount rule (Status=Inactive, IsActive=false). " +
    "Throws CustomerDiscountRuleNotFound if the rule does not exist.")]
public sealed class DeactivateCustomerDiscountRuleCommandHandler
    : AizenCommandHandler<DeactivateCustomerDiscountRuleCommand, DeactivateCustomerDiscountRuleResult>
{
    private readonly ICustomerDiscountRuleRepository _rules;
    private readonly ILogger<DeactivateCustomerDiscountRuleCommandHandler> _logger;

    public DeactivateCustomerDiscountRuleCommandHandler(
        ICustomerDiscountRuleRepository rules, ILogger<DeactivateCustomerDiscountRuleCommandHandler> logger)
    {
        _rules  = rules;
        _logger = logger;
    }

    public override async Task<DeactivateCustomerDiscountRuleResult?> Handle(
        DeactivateCustomerDiscountRuleCommand request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CustomerDiscountRuleNotFound);

        rule.Deactivate();
        _rules.Update(rule);
        _logger.LogInformation("Customer discount rule deactivated. Id={Id}", rule.Id);
        return new DeactivateCustomerDiscountRuleResult(rule.Id, rule.RuleCode);
    }
}
