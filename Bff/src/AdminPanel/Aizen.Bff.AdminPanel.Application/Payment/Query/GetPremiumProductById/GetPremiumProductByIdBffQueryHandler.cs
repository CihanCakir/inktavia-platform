using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPremiumProductById;

[DocumentationInfo("Get premium product by id BFF query handler (P11)",
    "Reads a single premium product (GET /admin/premium/products/{id}). Read-only.")]
public sealed class GetPremiumProductByIdBffQueryHandler
    : AizenQueryHandler<GetPremiumProductByIdBffQuery, GetPremiumProductByIdBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetPremiumProductByIdBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetPremiumProductByIdBffResponse?> Handle(GetPremiumProductByIdBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.GetPremiumProductByIdAsync(request.Id, ct) };
}
