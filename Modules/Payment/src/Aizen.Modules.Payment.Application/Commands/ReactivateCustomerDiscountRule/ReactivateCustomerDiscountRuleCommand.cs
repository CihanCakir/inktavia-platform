using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.ReactivateCustomerDiscountRule;

public sealed class ReactivateCustomerDiscountRuleCommand : AizenCommand<ReactivateCustomerDiscountRuleResult>
{
    public required long Id { get; init; }
}

public sealed record ReactivateCustomerDiscountRuleResult(long Id, string? RuleCode, string Status);
