using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Commands.UpdateCommissionRule;

public sealed class UpdateCommissionRuleCommand : AizenCommand<UpdateCommissionRuleResult>
{
    public long                   Id             { get; init; }
    public decimal                CommissionRate { get; init; }
    public DateTime               EffectiveFrom  { get; init; }
    public DateTime?              EffectiveTo    { get; init; }
    public CommissionRulePriority Priority       { get; init; }
    public string?                Notes          { get; init; }
}

public sealed record UpdateCommissionRuleResult(long Id, string? RuleCode);
