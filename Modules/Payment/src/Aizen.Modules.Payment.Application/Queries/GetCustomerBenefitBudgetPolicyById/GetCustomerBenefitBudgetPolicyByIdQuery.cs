using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetCustomerBenefitBudgetPolicyById;

public sealed class GetCustomerBenefitBudgetPolicyByIdQuery : AizenQuery<CustomerBenefitBudgetPolicyDto>
{
    public long Id { get; init; }
}
