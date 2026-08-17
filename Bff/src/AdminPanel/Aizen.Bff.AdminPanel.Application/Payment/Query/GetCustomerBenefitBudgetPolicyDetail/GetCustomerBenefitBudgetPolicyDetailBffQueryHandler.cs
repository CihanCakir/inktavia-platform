using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetCustomerBenefitBudgetPolicyDetail;

[DocumentationInfo("Get customer-benefit budget policy detail BFF query handler (BE-P6)",
    "Fetches a single benefit budget policy by ID from the Payment module (GET /benefit-budget/policies/{id}). " +
    "A CustomerBenefitBudgetNotFound surfaces through the envelope. Read-only.")]
public sealed class GetCustomerBenefitBudgetPolicyDetailBffQueryHandler
    : AizenQueryHandler<GetCustomerBenefitBudgetPolicyDetailBffQuery, GetCustomerBenefitBudgetPolicyDetailBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetCustomerBenefitBudgetPolicyDetailBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetCustomerBenefitBudgetPolicyDetailBffResponse?> Handle(GetCustomerBenefitBudgetPolicyDetailBffQuery request, CancellationToken ct)
        => new() { Policy = await _payment.GetCustomerBenefitBudgetPolicyDetailAsync(request.Id, ct) };
}
