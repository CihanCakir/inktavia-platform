using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetCustomerBenefitBudgetPoliciesList;

[DocumentationInfo("Get customer-benefit budget policies list BFF query handler (BE-P6)",
    "Returns the per-plan benefit budget policy list (no paging) with optional plan / currency / active filters, " +
    "forwarded to the Payment module. Read-only.")]
public sealed class GetCustomerBenefitBudgetPoliciesListBffQueryHandler
    : AizenQueryHandler<GetCustomerBenefitBudgetPoliciesListBffQuery, GetCustomerBenefitBudgetPoliciesListBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetCustomerBenefitBudgetPoliciesListBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetCustomerBenefitBudgetPoliciesListBffResponse?> Handle(GetCustomerBenefitBudgetPoliciesListBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ListCustomerBenefitBudgetPoliciesAsync(
            request.CustomerPlanId, request.CurrencyCode, request.IsActive, ct) };
}
