using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderCommissionBenefitEntitlementById;

[DocumentationInfo("GetProviderCommissionBenefitEntitlementByIdQueryHandler",
    "Returns the full detail of a single provider commission-benefit entitlement by ID (enriched with the referenced " +
    "benefit rule code/name). Throws PaymentErrorCode.ProviderCommissionBenefitEntitlementNotFound if it does not exist.")]
public sealed class GetProviderCommissionBenefitEntitlementByIdQueryHandler
    : AizenQueryHandler<GetProviderCommissionBenefitEntitlementByIdQuery, ProviderCommissionBenefitEntitlementDto>
{
    private readonly IProviderCommissionBenefitEntitlementRepository _entitlements;
    private readonly IProviderCommissionBenefitRuleRepository        _rules;

    public GetProviderCommissionBenefitEntitlementByIdQueryHandler(
        IProviderCommissionBenefitEntitlementRepository entitlements,
        IProviderCommissionBenefitRuleRepository        rules)
    {
        _entitlements = entitlements;
        _rules        = rules;
    }

    public override async Task<ProviderCommissionBenefitEntitlementDto?> Handle(
        GetProviderCommissionBenefitEntitlementByIdQuery request, CancellationToken ct)
    {
        var entitlement = await _entitlements.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitEntitlementNotFound);

        var rule = await _rules.GetByIdAsync(entitlement.BenefitRuleId, ct);
        return ProviderCommissionBenefitEntitlementDtoMapper.ToDto(entitlement, rule);
    }
}
