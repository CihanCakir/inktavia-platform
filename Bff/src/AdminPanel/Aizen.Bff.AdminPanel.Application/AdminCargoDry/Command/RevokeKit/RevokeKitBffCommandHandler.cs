using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.RevokeKit;

[DocumentationInfo("Revoke kit BFF command handler", "Calls the CargoDry admin kit revoke endpoint and returns the updated kit status.")]
public sealed class RevokeKitBffCommandHandler
    : AizenCommandHandler<RevokeKitBffCommand, RevokeKitBffCommandResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public RevokeKitBffCommandHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<RevokeKitBffCommandResponse?> Handle(
        RevokeKitBffCommand request, CancellationToken ct)
    {
        var result = await _remote.RevokeKitAsync(
            request.KitId,
            new RevokeKitBffRequest { Reason = request.Reason },
            ct);

        return new RevokeKitBffCommandResponse { Result = result };
    }
}
