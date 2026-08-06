using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreatePremiumProductPrice;


// ─── Create price ────────────────────────────────────────────────────────────
public sealed class CreatePremiumProductPriceBffCommand : AizenCommand<CreatePremiumProductPriceBffResponse>
{
    public CreatePremiumProductPriceBffRequest Body { get; init; } = default!;
}
public sealed class CreatePremiumProductPriceBffResponse { public PremiumMutateResultDto Result { get; init; } = default!; }
