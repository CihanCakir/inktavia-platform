using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.AdjustProviderBalance;


// ─── Adjust balance ──────────────────────────────────────────────────────────
public sealed class AdjustProviderBalanceBffCommand : AizenCommand<AdjustProviderBalanceBffResponse>
{
    public long                           ProviderProfileId { get; init; }
    public AdjustProviderBalanceBffRequest Body             { get; init; } = default!;
}
public sealed class AdjustProviderBalanceBffResponse { public ProviderBalanceAdjustResultDto Result { get; init; } = default!; }
