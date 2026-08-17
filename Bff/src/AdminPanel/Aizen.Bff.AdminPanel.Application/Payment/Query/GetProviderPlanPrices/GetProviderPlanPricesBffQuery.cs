using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderPlanPrices;


// ─── List prices for a plan ──────────────────────────────────────────────────
public sealed class GetProviderPlanPricesBffQuery : AizenQuery<GetProviderPlanPricesBffResponse>
{
    public long ProviderPlanId { get; init; }
}
public sealed class GetProviderPlanPricesBffResponse { public List<ProviderPlanPriceBffDto> Items { get; init; } = new(); }
