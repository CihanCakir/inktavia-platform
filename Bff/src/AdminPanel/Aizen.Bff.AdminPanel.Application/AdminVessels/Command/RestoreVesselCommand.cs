using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

public sealed class RestoreVesselCommand : AizenCommand<RestoreVesselResponse>
{
    public long VesselId { get; }
    public string Authorization { get; }
    public RestoreVesselCommand(long vesselId, string authorization)
    {
        VesselId = vesselId;
        Authorization = authorization;
    }
}

[DocumentationInfo("Restore vessel command handler", "Restores an archived vessel via the Vessel module.")]
public sealed class RestoreVesselCommandHandler
    : AizenCommandHandler<RestoreVesselCommand, RestoreVesselResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;

    public RestoreVesselCommandHandler(IVesselAdminBffRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<RestoreVesselResponse?> Handle(
        RestoreVesselCommand request, CancellationToken cancellationToken)
    {
        var result = await _vessel.RestoreVessel(request.VesselId, request.Authorization);
        return result.Body;
    }
}
