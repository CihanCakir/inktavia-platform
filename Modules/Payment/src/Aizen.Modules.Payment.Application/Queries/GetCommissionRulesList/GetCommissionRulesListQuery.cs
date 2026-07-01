using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetCommissionRulesList;

public sealed class GetCommissionRulesListQuery : AizenQuery<CommissionRuleListResult>
{
    public CommissionRuleType?     RuleType { get; init; }
    public CommissionRuleStatus?   Status   { get; init; }
    public CommissionRulePriority? Priority { get; init; }
    public int                     Page     { get; init; } = 1;
    public int                     PageSize { get; init; } = 25;
}
