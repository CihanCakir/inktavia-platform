using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetCustomerDiscountRuleDetail;

[DocumentationInfo("Get customer-discount rule detail BFF query handler (BE-P6)",
    "Fetches a single customer-discount rule by ID from the Payment module (GET /customer-discounts/rules/{id}). " +
    "A CustomerDiscountRuleNotFound surfaces through the envelope. Read-only.")]
public sealed class GetCustomerDiscountRuleDetailBffQueryHandler
    : AizenQueryHandler<GetCustomerDiscountRuleDetailBffQuery, GetCustomerDiscountRuleDetailBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetCustomerDiscountRuleDetailBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetCustomerDiscountRuleDetailBffResponse?> Handle(GetCustomerDiscountRuleDetailBffQuery request, CancellationToken ct)
        => new() { Rule = await _payment.GetCustomerDiscountRuleDetailAsync(request.Id, ct) };
}
