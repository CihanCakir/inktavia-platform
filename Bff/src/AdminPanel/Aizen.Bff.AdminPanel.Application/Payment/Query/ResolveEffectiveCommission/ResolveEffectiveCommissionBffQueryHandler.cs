using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.ResolveEffectiveCommission;

[DocumentationInfo("Resolve effective commission BFF query handler (BE-P7)",
    "Point-in-time effective-commission (base + benefit adjustments) preview (GET /commission-benefits/resolve). Read-only.")]
public sealed class ResolveEffectiveCommissionBffQueryHandler
    : AizenQueryHandler<ResolveEffectiveCommissionBffQuery, ResolveEffectiveCommissionBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ResolveEffectiveCommissionBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ResolveEffectiveCommissionBffResponse?> Handle(ResolveEffectiveCommissionBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolveEffectiveCommissionAsync(
            request.ProviderProfileId, request.ProviderPlanId, request.CategoryCode, request.ServiceAmount,
            request.CurrencyCode, request.EligibleGmvRemaining, request.PlanFloorRate, ct) };
}
