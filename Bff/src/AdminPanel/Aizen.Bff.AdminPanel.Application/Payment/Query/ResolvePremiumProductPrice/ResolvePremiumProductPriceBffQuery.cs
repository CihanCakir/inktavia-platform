using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.ResolvePremiumProductPrice;


// ─── Resolve price (point-in-time) ───────────────────────────────────────────
public sealed class ResolvePremiumProductPriceBffQuery : AizenQuery<ResolvePremiumProductPriceBffResponse>
{
    public long      ProductId    { get; init; }
    public string    CurrencyCode { get; init; } = "TRY";
    public DateTime? AtUtc        { get; init; }
}
public sealed class ResolvePremiumProductPriceBffResponse { public PremiumProductPriceAdminDto? Result { get; init; } }
