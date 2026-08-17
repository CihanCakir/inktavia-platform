using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPlatformFeeRuleById;

[DocumentationInfo("GetPlatformFeeRuleByIdQueryHandler",
    "Returns the full detail of a single platform fee rule by ID. " +
    "Throws PaymentErrorCode.PlatformFeeRuleNotFound if the rule does not exist.")]
public sealed class GetPlatformFeeRuleByIdQueryHandler
    : AizenQueryHandler<GetPlatformFeeRuleByIdQuery, PlatformFeeRuleDto>
{
    private readonly IPlatformFeeRuleRepository _rules;

    public GetPlatformFeeRuleByIdQueryHandler(IPlatformFeeRuleRepository rules)
        => _rules = rules;

    public override async Task<PlatformFeeRuleDto?> Handle(
        GetPlatformFeeRuleByIdQuery request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PlatformFeeRuleNotFound);

        return PlatformFeeRuleDtoMapper.ToDto(rule);
    }
}
