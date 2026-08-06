using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetCommissionRuleDetail;

public sealed class GetCommissionRuleDetailBffQuery : AizenQuery<GetCommissionRuleDetailBffResponse>
{
    public long Id { get; init; }
}

public sealed class GetCommissionRuleDetailBffResponse
{
    public CommissionRuleBffDto? Rule { get; init; }
}
