using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Commands.CreateCustomerDiscountRule;

public sealed class CreateCustomerDiscountRuleCommand : AizenCommand<CreateCustomerDiscountRuleResult>
{
    public long?                       CustomerPlanId          { get; init; }
    public string?                     CategoryCode            { get; init; }
    public string                      CurrencyCode            { get; init; } = "TRY";
    public CustomerDiscountType        DiscountType            { get; init; }
    public decimal?                    DiscountRate            { get; init; }
    public decimal?                    FixedDiscountAmount     { get; init; }
    public decimal?                    MinimumPurchaseAmount   { get; init; }
    public decimal?                    MaximumDiscountAmount   { get; init; }
    public CustomerDiscountFundingMode FundingMode             { get; init; }
    public decimal?                    PlatformFundingRate     { get; init; }
    public decimal?                    ProviderFundingRate     { get; init; }
    public bool?                       RequiresProviderConsent { get; init; }
    public CommissionRulePriority      Priority                { get; init; } = CommissionRulePriority.Standard;
    public required DateTime           EffectiveFrom           { get; init; }
    public DateTime?                   EffectiveTo             { get; init; }
    public string?                     RuleName                { get; init; }
    public string?                     Notes                   { get; init; }
}

public sealed record CreateCustomerDiscountRuleResult(long Id, string RuleCode);
