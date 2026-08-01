using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProfitProtectionPolicyById;

[DocumentationInfo("GetProfitProtectionPolicyByIdQueryHandler",
    "Returns the full detail of a single profit-protection policy by ID. " +
    "Throws PaymentErrorCode.ProfitProtectionPolicyNotFound if the policy does not exist.")]
public sealed class GetProfitProtectionPolicyByIdQueryHandler
    : AizenQueryHandler<GetProfitProtectionPolicyByIdQuery, ProfitProtectionPolicyDto>
{
    private readonly IProfitProtectionPolicyRepository _policies;

    public GetProfitProtectionPolicyByIdQueryHandler(IProfitProtectionPolicyRepository policies)
        => _policies = policies;

    public override async Task<ProfitProtectionPolicyDto?> Handle(
        GetProfitProtectionPolicyByIdQuery request, CancellationToken ct)
    {
        var policy = await _policies.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProfitProtectionPolicyNotFound);

        return ProfitProtectionPolicyDtoMapper.ToDto(policy);
    }
}
