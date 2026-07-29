using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CreatePlatformFeeRule;

[DocumentationInfo("CreatePlatformFeeRuleCommandHandler",
    "Admin creates a platform fee rule. Generates a unique RuleCode (PFR-YYYY-XXX), validates model coherence " +
    "via the domain factory, and runs the §7 fail-loud conflict guard before persisting.")]
public sealed class CreatePlatformFeeRuleCommandHandler
    : AizenCommandHandler<CreatePlatformFeeRuleCommand, CreatePlatformFeeRuleResult>
{
    private readonly IPlatformFeeRuleRepository _rules;
    private readonly ILogger<CreatePlatformFeeRuleCommandHandler> _logger;

    public CreatePlatformFeeRuleCommandHandler(
        IPlatformFeeRuleRepository rules,
        ILogger<CreatePlatformFeeRuleCommandHandler> logger)
    {
        _rules  = rules;
        _logger = logger;
    }

    public override async Task<CreatePlatformFeeRuleResult?> Handle(
        CreatePlatformFeeRuleCommand request, CancellationToken ct)
    {
        var ruleCode = await _rules.GenerateRuleCodeAsync(ct);

        // Domain factory validates model coherence (throws PlatformFeeRuleInvalid).
        var rule = PlatformFeeRuleEntity.Create(
            request.Model,
            request.Rate,
            request.FixedAmount,
            request.MinAmount,
            request.MaxAmount,
            request.CurrencyCode,
            request.CategoryCode,
            request.CustomerType,
            request.Priority,
            request.EffectiveFrom.ToUniversalTime(),
            request.EffectiveTo?.ToUniversalTime(),
            ruleCode,
            request.RuleName,
            request.Notes,
            request.VatRate);

        // §7 fail-loud conflict guard: reject an overlapping active rule with the same scope-key + priority.
        var conflict = await _rules.FindOverlappingActiveRuleAsync(rule, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.PlatformFeeRuleConflict,
                $"A conflicting active platform fee rule already exists (Id={conflict.Id}, RuleCode={conflict.RuleCode}) " +
                "with the same scope, priority, and an overlapping effective window.");

        await _rules.AddAsync(rule, ct);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Platform fee rule created. RuleCode={Code} Model={Model} Rate={Rate}",
            ruleCode, request.Model, request.Rate);

        return new CreatePlatformFeeRuleResult(rule.Id, ruleCode);
    }
}
