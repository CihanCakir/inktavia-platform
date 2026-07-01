using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetCommissionRulesList;

public sealed class GetCommissionRulesListBffQuery : AizenQuery<GetCommissionRulesListBffResponse>
{
    public string? RuleType  { get; init; }
    public string? Status    { get; init; }
    public string? Priority  { get; init; }
    public int     Page      { get; init; } = 1;
    public int     PageSize  { get; init; } = 20;
}

public sealed class GetCommissionRulesListBffResponse
{
    public CommissionRuleListBffResult Result { get; init; } = default!;
}
