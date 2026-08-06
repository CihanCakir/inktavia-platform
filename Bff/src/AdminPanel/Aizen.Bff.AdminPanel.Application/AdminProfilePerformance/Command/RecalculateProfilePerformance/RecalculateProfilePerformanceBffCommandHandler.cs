using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Command.RecalculateProfilePerformance;

[DocumentationInfo("Recalculate profile performance BFF command handler",
    "Triggers a full score recalculation for the given provider profile via the Profile module. " +
    "Phase 20 rule: BFF is proxy-only — no score calculation here.")]
public sealed class RecalculateProfilePerformanceBffCommandHandler
    : AizenCommandHandler<RecalculateProfilePerformanceBffCommand, RecalculateProfilePerformanceBffCommandResponse>
{
    private readonly IProfilePerformanceRemoteCall _remote;

    public RecalculateProfilePerformanceBffCommandHandler(IProfilePerformanceRemoteCall remote)
        => _remote = remote;

    public override async Task<RecalculateProfilePerformanceBffCommandResponse?> Handle(
        RecalculateProfilePerformanceBffCommand request, CancellationToken ct)
    {
        var result = await _remote.RecalculateAsync(request.ProfileId, request.ProfileType, request.Reason, ct);
        return new RecalculateProfilePerformanceBffCommandResponse { Result = result };
    }
}
