using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Command.ResolveProfileRiskSignal;

[DocumentationInfo("Resolve profile risk signal BFF command handler",
    "Resolves an active risk signal by id via the Profile module. " +
    "If no High/Critical signals remain after resolution, the module layer restores the tier " +
    "from the current snapshot score. " +
    "Phase 20 rule: BFF is proxy-only — no tier logic here.")]
public sealed class ResolveProfileRiskSignalBffCommandHandler
    : AizenCommandHandler<ResolveProfileRiskSignalBffCommand, ResolveProfileRiskSignalBffCommandResponse>
{
    private readonly IProfilePerformanceRemoteCall _remote;

    public ResolveProfileRiskSignalBffCommandHandler(IProfilePerformanceRemoteCall remote)
        => _remote = remote;

    public override async Task<ResolveProfileRiskSignalBffCommandResponse?> Handle(
        ResolveProfileRiskSignalBffCommand request, CancellationToken ct)
    {
        var result = await _remote.ResolveRiskSignalAsync(request.SignalId, request.Body, ct);
        return new ResolveProfileRiskSignalBffCommandResponse { Result = result };
    }
}
