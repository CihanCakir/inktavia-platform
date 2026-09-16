using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.ResolveCargoDrySalesAttributionFinancials;

[DocumentationInfo("Resolve attribution financials BFF command handler",
    "Forwards the resolve-financials request for a CargoDry sales attribution to the CargoDry admin " +
    "commercial endpoint. Resolves SalePrice, CommissionRate, ProviderShareAmount, and PlatformShareAmount " +
    "on the attribution record. Phase 4A (July 2026).")]
public sealed class ResolveCargoDrySalesAttributionFinancialsBffCommandHandler
    : AizenCommandHandler<ResolveCargoDrySalesAttributionFinancialsBffCommand,
                          ResolveCargoDrySalesAttributionFinancialsBffCommandResponse>
{
    private readonly ICargoDryRemoteCall    _remote;
    private readonly IAdminIdentityResolver  _resolver;
    private readonly IAdminIdentityHolder    _holder;

    public ResolveCargoDrySalesAttributionFinancialsBffCommandHandler(
        ICargoDryRemoteCall remote, IAdminIdentityResolver resolver, IAdminIdentityHolder holder)
    {
        _remote   = remote;
        _resolver = resolver;
        _holder   = holder;
    }

    public override async Task<ResolveCargoDrySalesAttributionFinancialsBffCommandResponse> Handle(
        ResolveCargoDrySalesAttributionFinancialsBffCommand request, CancellationToken ct)
    {
        // FIX_RESOLVE_FINANCIALS_ACTOR: the module validates ResolvedByUserId as a real user id, but the FE never
        // sends one and the body default (0) failed validation -> 500 at the module, generic toast at the FE.
        // Stamp the acting admin server-side (same IAdminIdentityResolver/Holder pattern as stock-request approve
        // and record-sale-retry); the client-supplied value is ignored on purpose - the actor id is not client data.
        await _resolver.ResolveAsync(ct);

        var remoteRequest = new ResolveAttributionFinancialsBffRequest
        {
            SalePrice              = request.SalePrice,
            CurrencyCode           = request.CurrencyCode,
            CommissionRateOverride = request.CommissionRateOverride,
            ResolvedByUserId       = _holder.UserId ?? 0,
            ResolutionNote         = request.ResolutionNote,
        };

        var result = await _remote.ResolveAttributionFinancialsAsync(
            request.SalesAttributionId, remoteRequest, ct);

        return new ResolveCargoDrySalesAttributionFinancialsBffCommandResponse
        {
            Attribution = result,
        };
    }
}
