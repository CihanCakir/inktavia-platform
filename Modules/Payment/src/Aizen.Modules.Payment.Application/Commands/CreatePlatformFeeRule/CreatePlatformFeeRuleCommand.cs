using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Commands.CreatePlatformFeeRule;

public sealed class CreatePlatformFeeRuleCommand : AizenCommand<CreatePlatformFeeRuleResult>
{
    public PlatformFeeModel       Model         { get; init; }
    public decimal?               Rate          { get; init; }
    public decimal?               FixedAmount   { get; init; }
    public decimal?               MinAmount     { get; init; }
    public decimal?               MaxAmount     { get; init; }
    public string                 CurrencyCode  { get; init; } = "TRY";
    public string?                CategoryCode  { get; init; }
    public string?                CustomerType  { get; init; }
    public CommissionRulePriority Priority      { get; init; } = CommissionRulePriority.Standard;
    public DateTime               EffectiveFrom { get; init; }
    public DateTime?              EffectiveTo   { get; init; }
    public string?                RuleName      { get; init; }
    public string?                Notes         { get; init; }
    public decimal?               VatRate       { get; init; }
}

public sealed record CreatePlatformFeeRuleResult(long Id, string RuleCode);
