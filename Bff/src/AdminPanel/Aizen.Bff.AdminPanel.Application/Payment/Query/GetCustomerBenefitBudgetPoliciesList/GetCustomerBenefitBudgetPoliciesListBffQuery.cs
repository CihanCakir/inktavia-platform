using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetCustomerBenefitBudgetPoliciesList;


// ─── CustomerBenefitBudgetPolicy: List (no paging) ───────────────────────────
public sealed class GetCustomerBenefitBudgetPoliciesListBffQuery : AizenQuery<GetCustomerBenefitBudgetPoliciesListBffResponse>
{
    public long?   CustomerPlanId { get; init; }
    public string? CurrencyCode   { get; init; }
    public bool?   IsActive       { get; init; }
}
public sealed class GetCustomerBenefitBudgetPoliciesListBffResponse { public CustomerBenefitBudgetPolicyListBffResult Result { get; init; } = default!; }
