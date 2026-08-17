using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPremiumProductPrices;


// ─── List prices for product ─────────────────────────────────────────────────
public sealed class GetPremiumProductPricesBffQuery : AizenQuery<GetPremiumProductPricesBffResponse>
{
    public long ProductId { get; init; }
}
public sealed class GetPremiumProductPricesBffResponse { public List<PremiumProductPriceAdminDto> Items { get; init; } = new(); }
