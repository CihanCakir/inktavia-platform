using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

[DocumentationInfo("Update vessel status command handler", "Changes the operational status of a vessel via the Vessel module.")]
public sealed class UpdateVesselStatusCommandHandler
    : AizenCommandHandler<UpdateVesselStatusCommand, UpdateVesselStatusResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;

    public UpdateVesselStatusCommandHandler(IVesselAdminBffRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<UpdateVesselStatusResponse?> Handle(
        UpdateVesselStatusCommand request, CancellationToken cancellationToken)
    {
        var result = await _vessel.UpdateVesselStatus(request.VesselId, request.Payload, request.Authorization, request.UserToken);
        return result.Body;
    }
}
