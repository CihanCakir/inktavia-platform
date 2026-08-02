using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderCommissionBenefitRuleById;

[DocumentationInfo("GetProviderCommissionBenefitRuleByIdQueryHandler",
    "Returns the full detail of a single provider commission-benefit rule by ID. " +
    "Throws PaymentErrorCode.ProviderCommissionBenefitRuleNotFound if the rule does not exist.")]
public sealed class GetProviderCommissionBenefitRuleByIdQueryHandler
    : AizenQueryHandler<GetProviderCommissionBenefitRuleByIdQuery, ProviderCommissionBenefitRuleDto>
{
    private readonly IProviderCommissionBenefitRuleRepository _rules;

    public GetProviderCommissionBenefitRuleByIdQueryHandler(IProviderCommissionBenefitRuleRepository rules)
        => _rules = rules;

    public override async Task<ProviderCommissionBenefitRuleDto?> Handle(
        GetProviderCommissionBenefitRuleByIdQuery request, CancellationToken ct)
    {
        var rule = await _rules.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitRuleNotFound);

        return ProviderCommissionBenefitRuleDtoMapper.ToDto(rule);
    }
}
