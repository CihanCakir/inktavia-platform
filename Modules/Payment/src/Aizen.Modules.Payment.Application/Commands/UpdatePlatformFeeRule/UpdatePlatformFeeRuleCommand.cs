using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Commands.UpdatePlatformFeeRule;

public sealed class UpdatePlatformFeeRuleCommand : AizenCommand<UpdatePlatformFeeRuleResult>
{
    public long                   Id            { get; init; }
    public PlatformFeeModel       Model         { get; init; }
    public decimal?               Rate          { get; init; }
    public decimal?               FixedAmount   { get; init; }
    public decimal?               MinAmount     { get; init; }
    public decimal?               MaxAmount     { get; init; }
    public CommissionRulePriority Priority      { get; init; }
    public DateTime               EffectiveFrom { get; init; }
    public DateTime?              EffectiveTo   { get; init; }
    public string?                RuleName      { get; init; }
    public string?                Notes         { get; init; }
    public decimal?               VatRate       { get; init; }
}

public sealed record UpdatePlatformFeeRuleResult(long Id, string? RuleCode);
