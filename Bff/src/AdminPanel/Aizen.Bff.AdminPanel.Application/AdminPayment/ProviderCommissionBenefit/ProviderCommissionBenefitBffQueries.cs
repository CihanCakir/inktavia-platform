using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.ProviderCommissionBenefit;

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

[DocumentationInfo("Resolve effective commission BFF query handler (BE-P7)",
    "Point-in-time effective-commission (base + benefit adjustments) preview (GET /commission-benefits/resolve). Read-only.")]
public sealed class ResolveEffectiveCommissionBffQueryHandler
    : AizenQueryHandler<ResolveEffectiveCommissionBffQuery, ResolveEffectiveCommissionBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public ResolveEffectiveCommissionBffQueryHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<ResolveEffectiveCommissionBffResponse?> Handle(ResolveEffectiveCommissionBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolveEffectiveCommissionAsync(
            request.ProviderProfileId, request.ProviderPlanId, request.CategoryCode, request.ServiceAmount,
            request.CurrencyCode, request.EligibleGmvRemaining, request.PlanFloorRate, ct) };
}
