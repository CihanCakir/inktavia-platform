using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetCustomerBenefitBudgetPolicyDetail;


// ─── CustomerBenefitBudgetPolicy: Detail (by id) ─────────────────────────────
public sealed class GetCustomerBenefitBudgetPolicyDetailBffQuery : AizenQuery<GetCustomerBenefitBudgetPolicyDetailBffResponse>
{
    public long Id { get; init; }
}
public sealed class GetCustomerBenefitBudgetPolicyDetailBffResponse { public CustomerBenefitBudgetPolicyDetailBffDto? Policy { get; init; } }
