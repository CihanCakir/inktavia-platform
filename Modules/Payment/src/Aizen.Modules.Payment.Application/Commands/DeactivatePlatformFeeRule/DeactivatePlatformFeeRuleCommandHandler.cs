using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.DeactivatePlatformFeeRule;

[DocumentationInfo("DeactivatePlatformFeeRuleCommandHandler",
    "Admin deactivates a platform fee rule (Status=Inactive, IsActive=false). " +
    "Throws PlatformFeeRuleNotFound if the rule does not exist.")]
public sealed class DeactivatePlatformFeeRuleCommandHandler
    : AizenCommandHandler<DeactivatePlatformFeeRuleCommand, DeactivatePlatformFeeRuleResult>
{
    private readonly IPlatformFeeRuleRepository _rules;
    private readonly ILogger<DeactivatePlatformFeeRuleCommandHandler> _logger;

    public DeactivatePlatformFeeRuleCommandHandler(
        IPlatformFeeRuleRepository rules,
        ILogger<DeactivatePlatformFeeRuleCommandHandler> logger)
    {
        _rules  = rules;
        _logger = logger;
    }

    public override async Task<DeactivatePlatformFeeRuleResult?> Handle(
        DeactivatePlatformFeeRuleCommand request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PlatformFeeRuleNotFound);

        rule.Deactivate();
        _rules.Update(rule);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation("Platform fee rule deactivated. Id={Id} RuleCode={Code}", rule.Id, rule.RuleCode);
        return new DeactivatePlatformFeeRuleResult(rule.Id, rule.RuleCode);
    }
}
