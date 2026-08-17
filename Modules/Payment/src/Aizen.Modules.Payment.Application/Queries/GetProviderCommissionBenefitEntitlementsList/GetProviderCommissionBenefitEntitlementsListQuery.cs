using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderCommissionBenefitEntitlementsList;

public sealed class GetProviderCommissionBenefitEntitlementsListQuery
    : AizenQuery<ProviderCommissionBenefitEntitlementListResult>
{
    public long? ProviderProfileId { get; init; }
    public long? BenefitRuleId     { get; init; }
    public bool? IsActive          { get; init; }
}
