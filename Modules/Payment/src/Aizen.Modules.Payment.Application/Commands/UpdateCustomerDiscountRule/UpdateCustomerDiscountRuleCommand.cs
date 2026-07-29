using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Commands.UpdateCustomerDiscountRule;

public sealed class UpdateCustomerDiscountRuleCommand : AizenCommand<UpdateCustomerDiscountRuleResult>
{
    public required long                Id                      { get; init; }
    public CustomerDiscountType         DiscountType            { get; init; }
    public decimal?                     DiscountRate            { get; init; }
    public decimal?                     FixedDiscountAmount     { get; init; }
    public decimal?                     MinimumPurchaseAmount   { get; init; }
    public decimal?                     MaximumDiscountAmount   { get; init; }
    public CustomerDiscountFundingMode  FundingMode             { get; init; }
    public decimal?                     PlatformFundingRate     { get; init; }
    public decimal?                     ProviderFundingRate     { get; init; }
    public bool                         RequiresProviderConsent { get; init; }
    public CommissionRulePriority       Priority                { get; init; }
    public required DateTime            EffectiveFrom           { get; init; }
    public DateTime?                    EffectiveTo             { get; init; }
    public string?                      RuleName                { get; init; }
    public string?                      Notes                   { get; init; }
}

public sealed record UpdateCustomerDiscountRuleResult(long Id, string? RuleCode);
