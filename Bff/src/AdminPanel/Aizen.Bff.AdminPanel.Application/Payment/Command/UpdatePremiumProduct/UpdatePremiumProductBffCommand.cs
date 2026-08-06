using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdatePremiumProduct;


// ─── Update product ──────────────────────────────────────────────────────────
public sealed class UpdatePremiumProductBffCommand : AizenCommand<UpdatePremiumProductBffResponse>
{
    public long                          Id   { get; init; }
    public UpdatePremiumProductBffRequest Body { get; init; } = default!;
}
public sealed class UpdatePremiumProductBffResponse { public PremiumMutateResultDto Result { get; init; } = default!; }
