using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Command.RaiseProfileRiskSignal;

[DocumentationInfo("Raise profile risk signal BFF command handler",
    "Raises a risk signal for a provider profile via the Profile module. " +
    "High/Critical signals immediately override the snapshot tier to Flagged in the module layer. " +
    "Phase 20 rule: BFF is proxy-only — no tier logic here.")]
public sealed class RaiseProfileRiskSignalBffCommandHandler
    : AizenCommandHandler<RaiseProfileRiskSignalBffCommand, RaiseProfileRiskSignalBffCommandResponse>
{
    private readonly IAdminProfilePerformanceBffRemoteCall _remote;

    public RaiseProfileRiskSignalBffCommandHandler(IAdminProfilePerformanceBffRemoteCall remote)
        => _remote = remote;

    public override async Task<RaiseProfileRiskSignalBffCommandResponse?> Handle(
        RaiseProfileRiskSignalBffCommand request, CancellationToken ct)
    {
        var result = await _remote.RaiseRiskSignalAsync(request.ProfileId, request.ProfileType, request.Body, ct);
        return new RaiseProfileRiskSignalBffCommandResponse { Result = result };
    }
}
