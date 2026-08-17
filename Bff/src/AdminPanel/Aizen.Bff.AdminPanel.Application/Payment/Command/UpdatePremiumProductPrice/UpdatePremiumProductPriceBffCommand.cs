using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdatePremiumProductPrice;


// ─── Update price ────────────────────────────────────────────────────────────
public sealed class UpdatePremiumProductPriceBffCommand : AizenCommand<UpdatePremiumProductPriceBffResponse>
{
    public long                               Id   { get; init; }
    public UpdatePremiumProductPriceBffRequest Body { get; init; } = default!;
}
public sealed class UpdatePremiumProductPriceBffResponse { public PremiumMutateResultDto Result { get; init; } = default!; }
