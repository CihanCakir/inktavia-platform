using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
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
    private readonly ICargoDryRemoteCall _remote;

    public ResolveMonthlySellThroughSettlementBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<ResolveMonthlySellThroughSettlementBffCommandResponse> Handle(
        ResolveMonthlySellThroughSettlementBffCommand request, CancellationToken ct)
    {
        var remoteRequest = new ResolveMonthlySettlementBffRequest
        {
            ResolvedByUserId = request.ResolvedByUserId,
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
