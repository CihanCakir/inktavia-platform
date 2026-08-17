using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderCommissionBenefitRuleById;

public sealed class GetProviderCommissionBenefitRuleByIdQuery : AizenQuery<ProviderCommissionBenefitRuleDto>
{
    public long Id { get; init; }
}
