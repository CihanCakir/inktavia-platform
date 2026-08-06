using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderBalance;

[DocumentationInfo("Get provider balance BFF query handler (P10)",
    "Reads a single provider's balance for a currency (GET /admin/provider-balances/{providerProfileId}). Read-only.")]
public sealed class GetProviderBalanceBffQueryHandler
    : AizenQueryHandler<GetProviderBalanceBffQuery, GetProviderBalanceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetProviderBalanceBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetProviderBalanceBffResponse?> Handle(GetProviderBalanceBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.GetProviderBalanceAsync(request.ProviderProfileId, request.Currency, ct) };
}
