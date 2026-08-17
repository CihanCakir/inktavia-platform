using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

[DocumentationInfo("UpdateVessel command handler", "Updates a vessel's details via the Vessel module.")]
public sealed class UpdateVesselBffCommandHandler : AizenCommandHandler<UpdateVesselBffCommand, UpdateVesselResponse>
{
    private readonly IVesselRemoteCall _vessel;
    public UpdateVesselBffCommandHandler(
        IVesselRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<UpdateVesselResponse?> Handle(UpdateVesselBffCommand request, CancellationToken ct)
    {

        var r = await _vessel.UpdateVessel(request.VesselId, request.Request);
        return r.Body;
    }
}
