using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.TransferKit;

[DocumentationInfo("Transfer kit BFF command handler",
    "Calls the CargoDry admin kit transfer endpoint. " +
    "Only Activated kits may be transferred; domain enforces this constraint.")]
public sealed class TransferCargoDryKitBffCommandHandler
    : AizenCommandHandler<TransferCargoDryKitBffCommand, TransferCargoDryKitBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public TransferCargoDryKitBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<TransferCargoDryKitBffCommandResponse?> Handle(
        TransferCargoDryKitBffCommand request, CancellationToken ct)
    {
        var result = await _remote.TransferKitAsync(
            request.KitId,
            new TransferKitBffRequest
            {
                NewUserId   = request.NewUserId,
                NewVesselId = request.NewVesselId,
            },
            ct);

        return new TransferCargoDryKitBffCommandResponse { Result = result };
    }
}
