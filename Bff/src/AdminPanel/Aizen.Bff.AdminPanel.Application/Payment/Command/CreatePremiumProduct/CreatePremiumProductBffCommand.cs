using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreatePremiumProduct;


// ─── Create product ──────────────────────────────────────────────────────────
public sealed class CreatePremiumProductBffCommand : AizenCommand<CreatePremiumProductBffResponse>
{
    public CreatePremiumProductBffRequest Body { get; init; } = default!;
}
public sealed class CreatePremiumProductBffResponse { public PremiumMutateResultDto Result { get; init; } = default!; }
