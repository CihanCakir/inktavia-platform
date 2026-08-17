using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.DeactivateCommissionRule;

public sealed class DeactivateCommissionRuleCommand : AizenCommand<DeactivateCommissionRuleResult>
{
    public long Id { get; init; }
}

public sealed record DeactivateCommissionRuleResult(long Id, string? RuleCode);
