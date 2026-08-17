using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.UpdateCustomerDiscountRule;

[DocumentationInfo("UpdateCustomerDiscountRuleCommandHandler",
    "Admin updates a customer discount rule. Re-validates funding/model coherence and runs the overlap conflict " +
    "guard (self excluded). Throws CustomerDiscountRuleNotFound if the rule does not exist.")]
public sealed class UpdateCustomerDiscountRuleCommandHandler
    : AizenCommandHandler<UpdateCustomerDiscountRuleCommand, UpdateCustomerDiscountRuleResult>
{
    private readonly ICustomerDiscountRuleRepository _rules;
    private readonly ILogger<UpdateCustomerDiscountRuleCommandHandler> _logger;

    public UpdateCustomerDiscountRuleCommandHandler(
        ICustomerDiscountRuleRepository rules, ILogger<UpdateCustomerDiscountRuleCommandHandler> logger)
    {
        _rules  = rules;
        _logger = logger;
    }

    public override async Task<UpdateCustomerDiscountRuleResult?> Handle(
        UpdateCustomerDiscountRuleCommand request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CustomerDiscountRuleNotFound);

        rule.Update(
            request.DiscountType, request.DiscountRate, request.FixedDiscountAmount,
            request.MinimumPurchaseAmount, request.MaximumDiscountAmount,
            request.FundingMode, request.PlatformFundingRate, request.ProviderFundingRate, request.RequiresProviderConsent,
            request.Priority, request.EffectiveFrom.ToUniversalTime(), request.EffectiveTo?.ToUniversalTime(),
            request.RuleName, request.Notes);

        var conflict = await _rules.FindOverlappingActiveRuleAsync(rule, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.CustomerDiscountRuleConflict,
                $"The update would conflict with an existing active rule (Id={conflict.Id}, Code={conflict.RuleCode}).");

        _rules.Update(rule);
        _logger.LogInformation("Customer discount rule updated. Id={Id}", rule.Id);
        return new UpdateCustomerDiscountRuleResult(rule.Id, rule.RuleCode);
    }
}
