using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivatePremiumProduct;


// ─── Deactivate product ──────────────────────────────────────────────────────
public sealed class DeactivatePremiumProductBffCommand : AizenCommand<DeactivatePremiumProductBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivatePremiumProductBffResponse { public PremiumMutateResultDto Result { get; init; } = default!; }
