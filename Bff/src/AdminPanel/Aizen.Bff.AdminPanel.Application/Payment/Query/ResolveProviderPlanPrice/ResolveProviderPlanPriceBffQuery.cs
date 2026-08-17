using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.ResolveProviderPlanPrice;


// ─── Resolve (point-in-time price) ───────────────────────────────────────────
public sealed class ResolveProviderPlanPriceBffQuery : AizenQuery<ResolveProviderPlanPriceBffResponse>
{
    public long      ProviderPlanId { get; init; }
    public string    CurrencyCode   { get; init; } = "TRY";
    public string    BillingPeriod  { get; init; } = "Monthly";
    public DateTime? AtUtc          { get; init; }
}
public sealed class ResolveProviderPlanPriceBffResponse { public ProviderPlanPriceBffDto? Result { get; init; } }
