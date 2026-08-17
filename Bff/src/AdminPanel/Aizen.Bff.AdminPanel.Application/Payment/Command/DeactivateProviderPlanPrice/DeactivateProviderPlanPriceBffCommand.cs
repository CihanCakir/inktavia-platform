using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivateProviderPlanPrice;


// ─── Deactivate ──────────────────────────────────────────────────────────────
public sealed class DeactivateProviderPlanPriceBffCommand : AizenCommand<DeactivateProviderPlanPriceBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivateProviderPlanPriceBffResponse { public ProviderPlanPriceMutateBffResult Result { get; init; } = default!; }
