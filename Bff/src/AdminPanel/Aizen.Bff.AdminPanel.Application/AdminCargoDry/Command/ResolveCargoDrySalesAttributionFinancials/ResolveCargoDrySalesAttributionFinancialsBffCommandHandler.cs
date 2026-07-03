using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.ResolveCargoDrySalesAttributionFinancials;

[DocumentationInfo("Resolve attribution financials BFF command handler",
    "Forwards the resolve-financials request for a CargoDry sales attribution to the CargoDry admin " +
    "commercial endpoint. Resolves SalePrice, CommissionRate, ProviderShareAmount, and PlatformShareAmount " +
    "on the attribution record. Phase 4A (July 2026).")]
public sealed class ResolveCargoDrySalesAttributionFinancialsBffCommandHandler
    : AizenCommandHandler<ResolveCargoDrySalesAttributionFinancialsBffCommand,
                          ResolveCargoDrySalesAttributionFinancialsBffCommandResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public ResolveCargoDrySalesAttributionFinancialsBffCommandHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<ResolveCargoDrySalesAttributionFinancialsBffCommandResponse> Handle(
        ResolveCargoDrySalesAttributionFinancialsBffCommand request, CancellationToken ct)
    {
        var remoteRequest = new ResolveAttributionFinancialsBffRequest
        {
            SalePrice              = request.SalePrice,
            CurrencyCode           = request.CurrencyCode,
            CommissionRateOverride = request.CommissionRateOverride,
            ResolvedByUserId       = request.ResolvedByUserId,
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
