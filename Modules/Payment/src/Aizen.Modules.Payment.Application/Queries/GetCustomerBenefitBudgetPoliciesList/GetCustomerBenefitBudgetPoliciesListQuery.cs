using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetCustomerBenefitBudgetPoliciesList;

public sealed class GetCustomerBenefitBudgetPoliciesListQuery : AizenQuery<CustomerBenefitBudgetPolicyListResult>
{
    public long?   CustomerPlanId { get; init; }
    public string? CurrencyCode   { get; init; }
    public bool?   IsActive       { get; init; }
}
