using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderBalance;


// ─── Single balance ──────────────────────────────────────────────────────────
public sealed class GetProviderBalanceBffQuery : AizenQuery<GetProviderBalanceBffResponse>
{
    public long   ProviderProfileId { get; init; }
    public string Currency          { get; init; } = "TRY";
}
public sealed class GetProviderBalanceBffResponse { public ProviderBalanceAdminDto? Result { get; init; } }
