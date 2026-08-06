using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetCustomerDiscountRuleDetail;


// ─── CustomerDiscountRule: Detail (by id) ────────────────────────────────────
public sealed class GetCustomerDiscountRuleDetailBffQuery : AizenQuery<GetCustomerDiscountRuleDetailBffResponse>
{
    public long Id { get; init; }
}
public sealed class GetCustomerDiscountRuleDetailBffResponse { public CustomerDiscountRuleDetailBffDto? Rule { get; init; } }
