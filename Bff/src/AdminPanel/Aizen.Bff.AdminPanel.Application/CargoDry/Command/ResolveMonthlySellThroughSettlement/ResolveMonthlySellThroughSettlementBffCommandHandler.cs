using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.ResolveMonthlySellThroughSettlement;

[DocumentationInfo("Resolve monthly settlement BFF command handler",
    "Forwards the resolve-monthly request to the CargoDry admin commercial settlements endpoint. " +
    "Verifies all attributions are financially resolved, recalculates totals, and marks the settlement " +
    "as ReadyForSettlement. Phase 4A (July 2026).")]
public sealed class ResolveMonthlySellThroughSettlementBffCommandHandler
    : AizenCommandHandler<ResolveMonthlySellThroughSettlementBffCommand,
                          ResolveMonthlySellThroughSettlementBffCommandResponse>
{
    private readonly ICargoDryRemoteCall    _remote;
    private readonly IAdminIdentityResolver  _resolver;
    private readonly IAdminIdentityHolder    _holder;

    public ResolveMonthlySellThroughSettlementBffCommandHandler(
        ICargoDryRemoteCall remote, IAdminIdentityResolver resolver, IAdminIdentityHolder holder)
    {
        _remote   = remote;
        _resolver = resolver;
        _holder   = holder;
    }

    public override async Task<ResolveMonthlySellThroughSettlementBffCommandResponse> Handle(
        ResolveMonthlySellThroughSettlementBffCommand request, CancellationToken ct)
    {
        // FIX_RESOLVE_FINANCIALS_ACTOR: same as attribution resolve-financials - the FE sends no
        // resolvedByUserId and the module rejects 0, so stamp the acting admin server-side.
        await _resolver.ResolveAsync(ct);

        var remoteRequest = new ResolveMonthlySettlementBffRequest
        {
            ResolvedByUserId = _holder.UserId ?? 0,
            ResolutionNote   = request.ResolutionNote,
        };

        var result = await _remote.ResolveMonthlySettlementAsync(
            request.SettlementId, remoteRequest, ct);

        return new ResolveMonthlySellThroughSettlementBffCommandResponse
        {
            Settlement = result,
        };
    }
}
