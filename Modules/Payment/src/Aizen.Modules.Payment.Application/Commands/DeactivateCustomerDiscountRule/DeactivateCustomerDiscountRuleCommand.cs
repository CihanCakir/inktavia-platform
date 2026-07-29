using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.DeactivateCustomerDiscountRule;

public sealed class DeactivateCustomerDiscountRuleCommand : AizenCommand<DeactivateCustomerDiscountRuleResult>
{
    public required long Id { get; init; }
}

public sealed record DeactivateCustomerDiscountRuleResult(long Id, string? RuleCode);
