using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateProviderPlanPrice;


// ─── Update ──────────────────────────────────────────────────────────────────
public sealed class UpdateProviderPlanPriceBffCommand : AizenCommand<UpdateProviderPlanPriceBffResponse>
{
    public long                             Id   { get; init; }
    public UpdateProviderPlanPriceBffRequest Body { get; init; } = default!;
}
public sealed class UpdateProviderPlanPriceBffResponse { public ProviderPlanPriceMutateBffResult Result { get; init; } = default!; }
