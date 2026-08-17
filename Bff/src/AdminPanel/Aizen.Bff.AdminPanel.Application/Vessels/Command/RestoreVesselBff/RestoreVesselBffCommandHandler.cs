using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

[DocumentationInfo("Restore vessel command handler", "Restores an archived vessel via the Vessel module.")]
public sealed class RestoreVesselBffCommandHandler
    : AizenCommandHandler<RestoreVesselBffCommand, RestoreVesselResponse>
{
    private readonly IVesselRemoteCall _vessel;

    public RestoreVesselBffCommandHandler(IVesselRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<RestoreVesselResponse?> Handle(
        RestoreVesselBffCommand request, CancellationToken cancellationToken)
    {

        var result = await _vessel.RestoreVessel(request.VesselId);
        return result.Body;
    }
}
