using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Commission;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CreateCommissionRule;

[DocumentationInfo("CreateCommissionRuleCommandHandler",
    "Admin creates a new commission rule. Generates a unique RuleCode (CR-YYYY-XXX), " +
    "selects the correct factory method based on RuleType, and persists the entity. " +
    "Initial Status is derived automatically from EffectiveFrom/EffectiveTo.")]
public sealed class CreateCommissionRuleCommandHandler
    : AizenCommandHandler<CreateCommissionRuleCommand, CreateCommissionRuleResult>
{
    private readonly ICommissionRuleRepository              _rules;
    private readonly ILogger<CreateCommissionRuleCommandHandler> _logger;

    public CreateCommissionRuleCommandHandler(
        ICommissionRuleRepository                       rules,
        ILogger<CreateCommissionRuleCommandHandler>     logger)
    {
        _rules  = rules;
        _logger = logger;
    }

    public override async Task<CreateCommissionRuleResult?> Handle(
        CreateCommissionRuleCommand request, CancellationToken ct)
    {
        var ruleCode = await _rules.GenerateRuleCodeAsync(ct);

        var rule = request.RuleType switch
        {
            CommissionRuleType.Global => CommissionRuleEntity.CreateGlobal(
                request.CommissionRate,
                request.EffectiveFrom.ToUniversalTime(),
                request.EffectiveTo?.ToUniversalTime(),
                request.Priority,
                request.Notes,
                ruleCode),

            CommissionRuleType.Category => CommissionRuleEntity.CreateForCategory(
                request.CategoryCode!,
                request.CommissionRate,
                request.EffectiveFrom.ToUniversalTime(),
                request.EffectiveTo?.ToUniversalTime(),
                request.Priority,
                request.Notes,
                ruleCode),

            CommissionRuleType.Plan => CommissionRuleEntity.CreateForPlan(
                request.ProviderPlanId!.Value,
                request.CommissionRate,
                request.EffectiveFrom.ToUniversalTime(),
                request.EffectiveTo?.ToUniversalTime(),
                request.Priority,
                request.Notes,
                ruleCode),

            CommissionRuleType.ProviderOverride => CommissionRuleEntity.CreateProviderOverride(
                request.ProviderProfileId!.Value,
                request.CommissionRate,
                request.EffectiveFrom.ToUniversalTime(),
                request.EffectiveTo?.ToUniversalTime(),
                request.Priority,
                request.Notes,
                ruleCode),

            _ => throw new ArgumentOutOfRangeException(nameof(request.RuleType), request.RuleType, null)
        };

        // Apply optional Phase 0 CargoDry targeting dimensions (allowed on any RuleType)
        if (request.ContextType.HasValue || request.ProductCode is not null || request.SalesChannel.HasValue)
            rule.SetContextDimensions(request.ContextType, request.ProductCode, request.SalesChannel);

        // Apply optional Phase 5 labeling fields
        if (request.RuleName is not null || request.CurrencyCode is not null || request.CommercialModel.HasValue)
            rule.SetMetadata(request.RuleName, request.CurrencyCode, request.CommercialModel);

        await _rules.AddAsync(rule, ct);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Commission rule created. RuleCode={Code} RuleType={Type} Rate={Rate}",
            ruleCode, request.RuleType, request.CommissionRate);

        return new CreateCommissionRuleResult(rule.Id, ruleCode);
    }
}
