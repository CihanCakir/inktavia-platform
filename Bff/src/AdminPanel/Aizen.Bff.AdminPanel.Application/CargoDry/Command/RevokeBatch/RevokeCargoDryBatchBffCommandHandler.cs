using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.RevokeBatch;

[DocumentationInfo("Revoke batch BFF command handler",
    "Calls the CargoDry admin batch revoke endpoint. " +
    "Revoking a batch also cascade-revokes all Available kits in that batch.")]
public sealed class RevokeCargoDryBatchBffCommandHandler
    : AizenCommandHandler<RevokeCargoDryBatchBffCommand, RevokeCargoDryBatchBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public RevokeCargoDryBatchBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<RevokeCargoDryBatchBffCommandResponse?> Handle(
        RevokeCargoDryBatchBffCommand request, CancellationToken ct)
    {
        var result = await _remote.RevokeBatchAsync(
            request.BatchCode,
            new RevokeBatchBffRequest { Reason = request.Reason },
            ct);

        return new RevokeCargoDryBatchBffCommandResponse { Result = result };
    }
}
