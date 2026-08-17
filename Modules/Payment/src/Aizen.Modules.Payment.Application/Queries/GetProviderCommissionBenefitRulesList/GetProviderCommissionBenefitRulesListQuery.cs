using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderCommissionBenefitRulesList;

public sealed class GetProviderCommissionBenefitRulesListQuery : AizenQuery<ProviderCommissionBenefitRuleListResult>
{
    public long?   ProviderProfileId { get; init; }
    public long?   ProviderPlanId    { get; init; }
    public string? CategoryCode      { get; init; }
    public string? CurrencyCode      { get; init; }
    public bool?   Stackable         { get; init; }
    public bool?   IsActive          { get; init; }
}
