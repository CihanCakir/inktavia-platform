using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreateProviderPlanPrice;


// ─── Create ──────────────────────────────────────────────────────────────────
public sealed class CreateProviderPlanPriceBffCommand : AizenCommand<CreateProviderPlanPriceBffResponse>
{
    public CreateProviderPlanPriceBffRequest Body { get; init; } = default!;
}
public sealed class CreateProviderPlanPriceBffResponse { public ProviderPlanPriceCreateBffResult Result { get; init; } = default!; }
