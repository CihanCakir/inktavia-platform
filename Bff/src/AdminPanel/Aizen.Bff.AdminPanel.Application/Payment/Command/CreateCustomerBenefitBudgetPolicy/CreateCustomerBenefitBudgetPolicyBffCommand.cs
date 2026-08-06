using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreateCustomerBenefitBudgetPolicy;


// ─── CustomerBenefitBudgetPolicy: Create (per plan) ──────────────────────────
public sealed class CreateCustomerBenefitBudgetPolicyBffCommand : AizenCommand<CreateCustomerBenefitBudgetPolicyBffResponse>
{
    public CreateCustomerBenefitBudgetPolicyBffRequest Body { get; init; } = default!;
}
public sealed class CreateCustomerBenefitBudgetPolicyBffResponse { public CustomerBenefitBudgetPolicyCreateBffResult Result { get; init; } = default!; }
