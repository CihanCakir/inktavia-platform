using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.UpdatePlatformFeeRule;

[DocumentationInfo("UpdatePlatformFeeRuleCommandHandler",
    "Admin updates a platform fee rule's model/parameters/lifecycle. Re-validates model coherence and runs the " +
    "§7 fail-loud conflict guard (self excluded). Throws PlatformFeeRuleNotFound if the rule does not exist.")]
public sealed class UpdatePlatformFeeRuleCommandHandler
    : AizenCommandHandler<UpdatePlatformFeeRuleCommand, UpdatePlatformFeeRuleResult>
{
    private readonly IPlatformFeeRuleRepository _rules;
    private readonly ILogger<UpdatePlatformFeeRuleCommandHandler> _logger;

    public UpdatePlatformFeeRuleCommandHandler(
        IPlatformFeeRuleRepository rules,
        ILogger<UpdatePlatformFeeRuleCommandHandler> logger)
    {
        _rules  = rules;
        _logger = logger;
    }

    public override async Task<UpdatePlatformFeeRuleResult?> Handle(
        UpdatePlatformFeeRuleCommand request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PlatformFeeRuleNotFound);

        // Re-validates model coherence (throws PlatformFeeRuleInvalid).
        rule.Update(
            request.Model,
            request.Rate,
            request.FixedAmount,
            request.MinAmount,
            request.MaxAmount,
            request.Priority,
            request.EffectiveFrom.ToUniversalTime(),
            request.EffectiveTo?.ToUniversalTime(),
            request.RuleName,
            request.Notes,
            request.VatRate);

        var conflict = await _rules.FindOverlappingActiveRuleAsync(rule, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.PlatformFeeRuleConflict,
                $"The update would conflict with an existing active platform fee rule (Id={conflict.Id}, " +
                $"RuleCode={conflict.RuleCode}) sharing the same scope, priority, and an overlapping window.");

        _rules.Update(rule);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation("Platform fee rule updated. Id={Id} RuleCode={Code}", rule.Id, rule.RuleCode);
        return new UpdatePlatformFeeRuleResult(rule.Id, rule.RuleCode);
    }
}
