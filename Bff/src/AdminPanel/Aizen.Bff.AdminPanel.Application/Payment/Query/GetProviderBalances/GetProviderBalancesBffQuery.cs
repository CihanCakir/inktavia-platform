using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderBalances;


// ─── List balances ───────────────────────────────────────────────────────────
public sealed class GetProviderBalancesBffQuery : AizenQuery<GetProviderBalancesBffResponse>
{
    public string? Currency     { get; init; }
    public bool    OnlyNegative { get; init; }
    public int     Page         { get; init; } = 1;
    public int     PageSize     { get; init; } = 20;
}
public sealed class GetProviderBalancesBffResponse { public ProviderBalancePagedDto? Result { get; init; } }
