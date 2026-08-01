using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetCustomerDiscountRuleById;

[DocumentationInfo("GetCustomerDiscountRuleByIdQueryHandler",
    "Returns the full detail of a single customer-discount rule by ID. " +
    "Throws PaymentErrorCode.CustomerDiscountRuleNotFound if the rule does not exist.")]
public sealed class GetCustomerDiscountRuleByIdQueryHandler
    : AizenQueryHandler<GetCustomerDiscountRuleByIdQuery, CustomerDiscountRuleDto>
{
    private readonly ICustomerDiscountRuleRepository _rules;

    public GetCustomerDiscountRuleByIdQueryHandler(ICustomerDiscountRuleRepository rules)
        => _rules = rules;

    public override async Task<CustomerDiscountRuleDto?> Handle(
        GetCustomerDiscountRuleByIdQuery request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CustomerDiscountRuleNotFound);

        return CustomerDiscountRuleDtoMapper.ToDto(rule);
    }
}
