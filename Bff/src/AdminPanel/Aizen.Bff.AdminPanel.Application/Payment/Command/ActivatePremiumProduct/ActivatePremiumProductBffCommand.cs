using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ActivatePremiumProduct;


// ─── Activate product ────────────────────────────────────────────────────────
public sealed class ActivatePremiumProductBffCommand : AizenCommand<ActivatePremiumProductBffResponse>
{
    public long Id { get; init; }
}
public sealed class ActivatePremiumProductBffResponse { public PremiumMutateResultDto Result { get; init; } = default!; }
