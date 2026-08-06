using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.ResolvePremiumProductPrice;

[DocumentationInfo("Resolve premium product price BFF query handler (P11)",
    "Point-in-time single-active premium price resolution (GET /admin/premium/products/{productId}/prices/resolve). Read-only.")]
public sealed class ResolvePremiumProductPriceBffQueryHandler
    : AizenQueryHandler<ResolvePremiumProductPriceBffQuery, ResolvePremiumProductPriceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ResolvePremiumProductPriceBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ResolvePremiumProductPriceBffResponse?> Handle(ResolvePremiumProductPriceBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolvePremiumProductPriceAsync(request.ProductId, request.CurrencyCode, request.AtUtc, ct) };
}
