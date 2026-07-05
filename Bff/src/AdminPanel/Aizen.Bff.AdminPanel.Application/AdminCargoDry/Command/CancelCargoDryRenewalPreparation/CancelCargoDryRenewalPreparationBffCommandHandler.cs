using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.CancelCargoDryRenewalPreparation;

[DocumentationInfo("Cancel CargoDry renewal preparation BFF command handler",
    "Calls the CargoDry admin renewals/{id}/cancel POST endpoint. " +
    "Phase 11 (July 2026).")]
public sealed class CancelCargoDryRenewalPreparationBffCommandHandler
    : AizenCommandHandler<CancelCargoDryRenewalPreparationBffCommand, CancelCargoDryRenewalPreparationBffCommandResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public CancelCargoDryRenewalPreparationBffCommandHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<CancelCargoDryRenewalPreparationBffCommandResponse?> Handle(
        CancelCargoDryRenewalPreparationBffCommand request, CancellationToken ct)
    {
        var result = await _remote.CancelRenewalPreparationAsync(
            request.RenewalPreparationId,
            new CancelRenewalPreparationBffRequest
            {
                CancellationReason = request.CancellationReason,
                Note               = request.Note,
            }, ct);

        return new CancelCargoDryRenewalPreparationBffCommandResponse { Preparation = result };
    }
}
