using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderBalances;

[DocumentationInfo("Get provider balances BFF query handler (P10)",
    "Paged provider-balance ledger with optional currency/only-negative filters (GET /admin/provider-balances). Read-only.")]
public sealed class GetProviderBalancesBffQueryHandler
    : AizenQueryHandler<GetProviderBalancesBffQuery, GetProviderBalancesBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetProviderBalancesBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetProviderBalancesBffResponse?> Handle(GetProviderBalancesBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.GetProviderBalancesAsync(
            request.Currency, request.OnlyNegative, request.Page, request.PageSize, ct) };
}
