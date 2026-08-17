using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.ResolveProviderPlanPrice;

[DocumentationInfo("Resolve provider-plan-price BFF query handler (BE-P4)",
    "Point-in-time single-active price resolution (GET /plan-prices/resolve). The resolver dev query.")]
public sealed class ResolveProviderPlanPriceBffQueryHandler
    : AizenQueryHandler<ResolveProviderPlanPriceBffQuery, ResolveProviderPlanPriceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ResolveProviderPlanPriceBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ResolveProviderPlanPriceBffResponse?> Handle(ResolveProviderPlanPriceBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolveProviderPlanPriceAsync(
            request.ProviderPlanId, request.CurrencyCode, request.BillingPeriod, request.AtUtc, ct) };
}
