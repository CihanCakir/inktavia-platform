using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetCommissionRuleDetail;

public sealed class GetCommissionRuleDetailBffQuery : AizenQuery<GetCommissionRuleDetailBffResponse>
{
    public long Id { get; init; }
}

public sealed class GetCommissionRuleDetailBffResponse
{
    public CommissionRuleBffDto? Rule { get; init; }
}
