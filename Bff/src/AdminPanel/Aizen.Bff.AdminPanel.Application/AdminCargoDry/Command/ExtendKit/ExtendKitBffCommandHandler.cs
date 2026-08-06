using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.ExtendKit;

[DocumentationInfo("Extend kit BFF command handler", "Calls the CargoDry admin kit extend endpoint and returns the updated kit after validity extension.")]
public sealed class ExtendKitBffCommandHandler
    : AizenCommandHandler<ExtendKitBffCommand, ExtendKitBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public ExtendKitBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<ExtendKitBffCommandResponse?> Handle(
        ExtendKitBffCommand request, CancellationToken ct)
    {
        var result = await _remote.ExtendKitAsync(
            request.KitId,
            new ExtendKitBffRequest { AddedDays = request.AddedDays },
            ct);

        return new ExtendKitBffCommandResponse { Kit = result };
    }
}
