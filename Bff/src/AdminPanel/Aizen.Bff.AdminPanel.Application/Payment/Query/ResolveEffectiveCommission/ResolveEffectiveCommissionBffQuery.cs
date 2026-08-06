using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.ResolveEffectiveCommission;


// ─── Resolve (effective commission preview) ──────────────────────────────────
public sealed class ResolveEffectiveCommissionBffQuery : AizenQuery<ResolveEffectiveCommissionBffResponse>
{
    public long      ProviderProfileId    { get; init; }
    public long?     ProviderPlanId       { get; init; }
    public string?   CategoryCode         { get; init; }
    public decimal   ServiceAmount        { get; init; }
    public string    CurrencyCode         { get; init; } = "TRY";
    public decimal?  EligibleGmvRemaining { get; init; }
    public decimal   PlanFloorRate        { get; init; }
}
public sealed class ResolveEffectiveCommissionBffResponse { public EffectiveCommissionResolveBffResult? Result { get; init; } }
