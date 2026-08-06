using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.PrepareCargoDryKitRenewal;

[DocumentationInfo("Prepare CargoDry kit renewal BFF command handler",
    "Calls the CargoDry admin renewals POST endpoint and returns the new preparation record. " +
    "Phase 11 (July 2026).")]
public sealed class PrepareCargoDryKitRenewalBffCommandHandler
    : AizenCommandHandler<PrepareCargoDryKitRenewalBffCommand, PrepareCargoDryKitRenewalBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public PrepareCargoDryKitRenewalBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<PrepareCargoDryKitRenewalBffCommandResponse?> Handle(
        PrepareCargoDryKitRenewalBffCommand request, CancellationToken ct)
    {
        var result = await _remote.PrepareRenewalAsync(
            new PrepareRenewalBffRequest
            {
                KitId                  = request.KitId,
                RequestedRenewalMonths = request.RequestedRenewalMonths,
                Note                   = request.Note,
            }, ct);

        return new PrepareCargoDryKitRenewalBffCommandResponse { Preparation = result };
    }
}
