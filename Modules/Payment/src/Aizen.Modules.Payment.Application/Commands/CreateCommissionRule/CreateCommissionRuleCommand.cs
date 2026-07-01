using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Commands.CreateCommissionRule;

public sealed class CreateCommissionRuleCommand : AizenCommand<CreateCommissionRuleResult>
{
    public CommissionRuleType     RuleType          { get; init; }
    public string?                CategoryCode      { get; init; }
    public long?                  ProviderPlanId    { get; init; }
    public long?                  ProviderProfileId { get; init; }
    public decimal                CommissionRate    { get; init; }
    public DateTime               EffectiveFrom     { get; init; }
    public DateTime?              EffectiveTo       { get; init; }
    public string?                Notes             { get; init; }
    public CommissionRulePriority Priority          { get; init; } = CommissionRulePriority.Standard;
}

public sealed record CreateCommissionRuleResult(long Id, string RuleCode);
