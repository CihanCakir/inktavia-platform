using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetCustomerDiscountRulesList;

[DocumentationInfo("Get customer-discount rules list BFF query handler (BE-P6)",
    "Returns the customer-discount rule list (no paging) with optional plan / category / currency / funding / active " +
    "filters, forwarded as plain strings to the Payment module. Read-only.")]
public sealed class GetCustomerDiscountRulesListBffQueryHandler
    : AizenQueryHandler<GetCustomerDiscountRulesListBffQuery, GetCustomerDiscountRulesListBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetCustomerDiscountRulesListBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetCustomerDiscountRulesListBffResponse?> Handle(GetCustomerDiscountRulesListBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ListCustomerDiscountRulesAsync(
            request.CustomerPlanId, request.CategoryCode, request.CurrencyCode, request.FundingMode, request.IsActive, ct) };
}
