using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.ResolveCustomerDiscount;

[DocumentationInfo("Resolve customer-discount BFF query handler (BE-P6)",
    "Point-in-time customer-discount + funding-split preview (GET /customer-discounts/resolve). Read-only.")]
public sealed class ResolveCustomerDiscountBffQueryHandler
    : AizenQueryHandler<ResolveCustomerDiscountBffQuery, ResolveCustomerDiscountBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ResolveCustomerDiscountBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ResolveCustomerDiscountBffResponse?> Handle(ResolveCustomerDiscountBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolveCustomerDiscountAsync(
            request.CustomerPlanId, request.CategoryCode, request.CurrencyCode,
            request.ServiceBaseAmount, request.ProviderConsent, request.ParticipantPlanSubscriptionId, ct) };
}
