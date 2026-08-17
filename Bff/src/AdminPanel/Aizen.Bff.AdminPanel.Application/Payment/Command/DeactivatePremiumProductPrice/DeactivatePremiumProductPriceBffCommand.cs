using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivatePremiumProductPrice;


// ─── Deactivate price ────────────────────────────────────────────────────────
public sealed class DeactivatePremiumProductPriceBffCommand : AizenCommand<DeactivatePremiumProductPriceBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivatePremiumProductPriceBffResponse { public PremiumMutateResultDto Result { get; init; } = default!; }
