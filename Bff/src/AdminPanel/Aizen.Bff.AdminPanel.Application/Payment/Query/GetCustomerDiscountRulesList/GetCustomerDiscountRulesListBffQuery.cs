using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetCustomerDiscountRulesList;


// ─── CustomerDiscountRule: List (no paging) ──────────────────────────────────
public sealed class GetCustomerDiscountRulesListBffQuery : AizenQuery<GetCustomerDiscountRulesListBffResponse>
{
    public long?   CustomerPlanId { get; init; }
    public string? CategoryCode   { get; init; }
    public string? CurrencyCode   { get; init; }
    public string? FundingMode    { get; init; }
    public bool?   IsActive       { get; init; }
}
public sealed class GetCustomerDiscountRulesListBffResponse { public CustomerDiscountRuleListBffResult Result { get; init; } = default!; }
