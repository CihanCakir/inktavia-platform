using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetCommissionRuleStats;

public sealed class GetCommissionRuleStatsBffQuery : AizenQuery<GetCommissionRuleStatsBffResponse>;

public sealed class GetCommissionRuleStatsBffResponse
{
    public CommissionRuleStatsBffDto? Stats { get; init; }
}
