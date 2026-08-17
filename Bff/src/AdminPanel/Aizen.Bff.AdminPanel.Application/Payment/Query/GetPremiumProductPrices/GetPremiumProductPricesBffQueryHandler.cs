using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPremiumProductPrices;

[DocumentationInfo("Get premium product prices BFF query handler (P11)",
    "Lists all versioned prices for a premium product (GET /admin/premium/products/{productId}/prices). Read-only.")]
public sealed class GetPremiumProductPricesBffQueryHandler
    : AizenQueryHandler<GetPremiumProductPricesBffQuery, GetPremiumProductPricesBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetPremiumProductPricesBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetPremiumProductPricesBffResponse?> Handle(GetPremiumProductPricesBffQuery request, CancellationToken ct)
        => new() { Items = await _payment.GetPremiumProductPricesAsync(request.ProductId, ct) };
}
