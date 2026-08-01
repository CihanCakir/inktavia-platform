using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetProfitProtectionPoliciesList;

public sealed class GetProfitProtectionPoliciesListQuery : AizenQuery<ProfitProtectionPolicyListResult>
{
    public string?               CurrencyCode { get; init; }
    public CommissionRuleStatus? Status       { get; init; }
    public bool?                 IsActive     { get; init; }
}
