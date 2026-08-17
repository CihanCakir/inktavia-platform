using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPremiumProducts;

[DocumentationInfo("Get premium products BFF query handler (P11)",
    "Lists all premium products (GET /admin/premium/products). Read-only.")]
public sealed class GetPremiumProductsBffQueryHandler
    : AizenQueryHandler<GetPremiumProductsBffQuery, GetPremiumProductsBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetPremiumProductsBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetPremiumProductsBffResponse?> Handle(GetPremiumProductsBffQuery request, CancellationToken ct)
        => new() { Items = await _payment.GetPremiumProductsAsync(ct) };
}
