using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetCustomerDiscountRuleById;

public sealed class GetCustomerDiscountRuleByIdQuery : AizenQuery<CustomerDiscountRuleDto>
{
    public long Id { get; init; }
}
