using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetCustomerBenefitBudgetPolicyById;

[DocumentationInfo("GetCustomerBenefitBudgetPolicyByIdQueryHandler",
    "Returns the full detail of a single customer-benefit budget policy by ID. " +
    "Throws PaymentErrorCode.CustomerBenefitBudgetNotFound if the policy does not exist.")]
public sealed class GetCustomerBenefitBudgetPolicyByIdQueryHandler
    : AizenQueryHandler<GetCustomerBenefitBudgetPolicyByIdQuery, CustomerBenefitBudgetPolicyDto>
{
    private readonly ICustomerBenefitBudgetPolicyRepository _policies;

    public GetCustomerBenefitBudgetPolicyByIdQueryHandler(ICustomerBenefitBudgetPolicyRepository policies)
        => _policies = policies;

    public override async Task<CustomerBenefitBudgetPolicyDto?> Handle(
        GetCustomerBenefitBudgetPolicyByIdQuery request, CancellationToken ct)
    {
        var policy = await _policies.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CustomerBenefitBudgetNotFound);

        return CustomerBenefitBudgetPolicyDtoMapper.ToDto(policy);
    }
}
