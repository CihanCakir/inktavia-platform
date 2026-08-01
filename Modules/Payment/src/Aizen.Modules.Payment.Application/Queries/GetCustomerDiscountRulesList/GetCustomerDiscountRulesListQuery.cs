using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetCustomerDiscountRulesList;

public sealed class GetCustomerDiscountRulesListQuery : AizenQuery<CustomerDiscountRuleListResult>
{
    public long?                        CustomerPlanId { get; init; }
    public string?                      CategoryCode   { get; init; }
    public string?                      CurrencyCode   { get; init; }
    public CustomerDiscountFundingMode? FundingMode    { get; init; }
    public bool?                        IsActive       { get; init; }
}
