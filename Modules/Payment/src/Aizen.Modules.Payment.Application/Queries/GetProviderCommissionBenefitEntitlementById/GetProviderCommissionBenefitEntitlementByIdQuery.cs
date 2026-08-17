using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderCommissionBenefitEntitlementById;

public sealed class GetProviderCommissionBenefitEntitlementByIdQuery : AizenQuery<ProviderCommissionBenefitEntitlementDto>
{
    public long Id { get; init; }
}
