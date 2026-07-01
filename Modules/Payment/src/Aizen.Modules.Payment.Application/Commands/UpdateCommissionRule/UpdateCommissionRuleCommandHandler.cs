using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.UpdateCommissionRule;

[DocumentationInfo("UpdateCommissionRuleCommandHandler",
    "Admin updates the mutable fields of an existing commission rule: " +
    "CommissionRate, EffectiveFrom/To, Priority, and Notes. " +
    "Status is re-derived automatically after the update. " +
    "Throws CommissionRuleNotFound if the rule does not exist.")]
public sealed class UpdateCommissionRuleCommandHandler
    : AizenCommandHandler<UpdateCommissionRuleCommand, UpdateCommissionRuleResult>
{
    private readonly ICommissionRuleRepository              _rules;
    private readonly ILogger<UpdateCommissionRuleCommandHandler> _logger;

    public UpdateCommissionRuleCommandHandler(
        ICommissionRuleRepository                        rules,
        ILogger<UpdateCommissionRuleCommandHandler>      logger)
    {
        _rules  = rules;
        _logger = logger;
    }

    public override async Task<UpdateCommissionRuleResult?> Handle(
        UpdateCommissionRuleCommand request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CommissionRuleNotFound);

        rule.Update(
            request.CommissionRate,
            request.EffectiveFrom.ToUniversalTime(),
            request.EffectiveTo?.ToUniversalTime(),
            request.Priority,
            request.Notes);

        _rules.Update(rule);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Commission rule updated. Id={Id} RuleCode={Code} Rate={Rate}",
            rule.Id, rule.RuleCode, request.CommissionRate);

        return new UpdateCommissionRuleResult(rule.Id, rule.RuleCode);
    }
}
