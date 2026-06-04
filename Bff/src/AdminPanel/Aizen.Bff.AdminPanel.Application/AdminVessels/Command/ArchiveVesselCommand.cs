using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

public sealed class ArchiveVesselCommand : AizenCommand<ArchiveVesselResponse>
{
    public long VesselId { get; }
    public ArchiveVesselRequest Payload { get; }
    public string Authorization { get; }
    public ArchiveVesselCommand(long vesselId, ArchiveVesselRequest payload, string authorization)
    {
        VesselId = vesselId;
        Payload = payload;
        Authorization = authorization;
    }
}

[DocumentationInfo("Archive vessel command handler", "Archives a vessel with a reason via the Vessel module.")]
public sealed class ArchiveVesselCommandHandler
    : AizenCommandHandler<ArchiveVesselCommand, ArchiveVesselResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;

    public ArchiveVesselCommandHandler(IVesselAdminBffRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<ArchiveVesselResponse?> Handle(
        ArchiveVesselCommand request, CancellationToken cancellationToken)
    {
        var result = await _vessel.ArchiveVessel(
            request.VesselId,
            request.Payload,
            request.Authorization);

        return result.Body;
    }
}
