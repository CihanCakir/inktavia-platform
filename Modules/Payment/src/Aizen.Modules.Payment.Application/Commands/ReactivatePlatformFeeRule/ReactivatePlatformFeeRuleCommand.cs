using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.ReactivatePlatformFeeRule;

public sealed class ReactivatePlatformFeeRuleCommand : AizenCommand<ReactivatePlatformFeeRuleResult>
{
    public long Id { get; init; }
}

public sealed record ReactivatePlatformFeeRuleResult(long Id, string? RuleCode, string Status);
