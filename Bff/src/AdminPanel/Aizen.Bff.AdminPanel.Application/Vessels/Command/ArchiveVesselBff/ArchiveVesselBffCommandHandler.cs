using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

[DocumentationInfo("Archive vessel command handler", "Archives a vessel with a reason via the Vessel module.")]
public sealed class ArchiveVesselBffCommandHandler
    : AizenCommandHandler<ArchiveVesselBffCommand, ArchiveVesselResponse>
{
    private readonly IVesselRemoteCall _vessel;

    public ArchiveVesselBffCommandHandler(IVesselRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<ArchiveVesselResponse?> Handle(
        ArchiveVesselBffCommand request, CancellationToken cancellationToken)
    {

        var result = await _vessel.ArchiveVessel(request.VesselId, request.Payload);
        return result.Body;
    }
}
