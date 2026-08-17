using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.CustomerDiscount;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CreateCustomerDiscountRule;

[DocumentationInfo("CreateCustomerDiscountRuleCommandHandler",
    "Admin creates a customer discount rule. Validates funding/model coherence via the domain factory (no rule " +
    "without a funding source) and runs the specificity/overlap conflict guard before persisting.")]
public sealed class CreateCustomerDiscountRuleCommandHandler
    : AizenCommandHandler<CreateCustomerDiscountRuleCommand, CreateCustomerDiscountRuleResult>
{
    private readonly ICustomerDiscountRuleRepository _rules;
    private readonly ILogger<CreateCustomerDiscountRuleCommandHandler> _logger;

    public CreateCustomerDiscountRuleCommandHandler(
        ICustomerDiscountRuleRepository rules, ILogger<CreateCustomerDiscountRuleCommandHandler> logger)
    {
        _rules  = rules;
        _logger = logger;
    }

    public override async Task<CreateCustomerDiscountRuleResult?> Handle(
        CreateCustomerDiscountRuleCommand request, CancellationToken ct)
    {
        var ruleCode = await _rules.GenerateCodeAsync(ct);

        // ProviderFunded defaults to requiring consent unless explicitly overridden.
        var requiresConsent = request.RequiresProviderConsent
            ?? request.FundingMode is CustomerDiscountFundingMode.ProviderFunded or CustomerDiscountFundingMode.Shared;

        var rule = CustomerDiscountRuleEntity.Create(
            request.CustomerPlanId, request.CategoryCode, request.CurrencyCode,
            request.DiscountType, request.DiscountRate, request.FixedDiscountAmount,
            request.MinimumPurchaseAmount, request.MaximumDiscountAmount,
            request.FundingMode, request.PlatformFundingRate, request.ProviderFundingRate, requiresConsent,
            request.Priority, request.EffectiveFrom.ToUniversalTime(), request.EffectiveTo?.ToUniversalTime(),
            ruleCode, request.RuleName, request.Notes);

        var conflict = await _rules.FindOverlappingActiveRuleAsync(rule, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.CustomerDiscountRuleConflict,
                $"A conflicting active customer discount rule already exists (Id={conflict.Id}, Code={conflict.RuleCode}).");

        await _rules.AddAsync(rule, ct);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation("Customer discount rule created. Code={Code} Funding={Funding}", ruleCode, request.FundingMode);
        return new CreateCustomerDiscountRuleResult(rule.Id, ruleCode);
    }
}
