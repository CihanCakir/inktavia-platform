using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.ReactivateProviderCommissionBenefitRule;

public sealed class ReactivateProviderCommissionBenefitRuleCommand : AizenCommand<ReactivateProviderCommissionBenefitRuleResult>
{
    public required long Id { get; init; }
}

public sealed record ReactivateProviderCommissionBenefitRuleResult(long Id, string? RuleCode, string Status);
