using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.DeactivatePlatformFeeRule;

public sealed class DeactivatePlatformFeeRuleCommand : AizenCommand<DeactivatePlatformFeeRuleResult>
{
    public long Id { get; init; }
}

public sealed record DeactivatePlatformFeeRuleResult(long Id, string? RuleCode);
