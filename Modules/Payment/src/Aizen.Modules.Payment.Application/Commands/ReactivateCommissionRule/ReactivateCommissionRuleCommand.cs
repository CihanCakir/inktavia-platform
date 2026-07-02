using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.ReactivateCommissionRule;

public sealed class ReactivateCommissionRuleCommand : AizenCommand<ReactivateCommissionRuleResult>
{
    public long Id { get; init; }
}

public sealed record ReactivateCommissionRuleResult(long Id, string? RuleCode, string Status);
