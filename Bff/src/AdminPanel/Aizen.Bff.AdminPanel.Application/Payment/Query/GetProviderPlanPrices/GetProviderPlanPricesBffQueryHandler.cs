using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderPlanPrices;

[DocumentationInfo("Get provider-plan prices BFF query handler (BE-P4)",
    "Lists all versioned prices for a plan (GET /plan-prices/plan/{planId}). Read-only.")]
public sealed class GetProviderPlanPricesBffQueryHandler
    : AizenQueryHandler<GetProviderPlanPricesBffQuery, GetProviderPlanPricesBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetProviderPlanPricesBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetProviderPlanPricesBffResponse?> Handle(GetProviderPlanPricesBffQuery request, CancellationToken ct)
        => new() { Items = await _payment.GetProviderPlanPricesForPlanAsync(request.ProviderPlanId, ct) };
}
