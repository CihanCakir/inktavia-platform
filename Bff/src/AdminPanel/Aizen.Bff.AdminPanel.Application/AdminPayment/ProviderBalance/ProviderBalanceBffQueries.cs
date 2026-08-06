using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.ProviderBalance;

// ─── List balances ───────────────────────────────────────────────────────────
public sealed class GetProviderBalancesBffQuery : AizenQuery<GetProviderBalancesBffResponse>
{
    public string? Currency     { get; init; }
    public bool    OnlyNegative { get; init; }
    public int     Page         { get; init; } = 1;
    public int     PageSize     { get; init; } = 20;
}
public sealed class GetProviderBalancesBffResponse { public ProviderBalancePagedDto? Result { get; init; } }

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

// ─── Single balance ──────────────────────────────────────────────────────────
public sealed class GetProviderBalanceBffQuery : AizenQuery<GetProviderBalanceBffResponse>
{
    public long   ProviderProfileId { get; init; }
    public string Currency          { get; init; } = "TRY";
}
public sealed class GetProviderBalanceBffResponse { public ProviderBalanceAdminDto? Result { get; init; } }

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
