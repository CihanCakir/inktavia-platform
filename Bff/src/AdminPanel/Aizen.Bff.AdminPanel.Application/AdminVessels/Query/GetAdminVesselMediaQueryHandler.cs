using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Media;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel media query handler", "Fetches media files for a specific vessel from the Vessel module.")]
public sealed class GetAdminVesselMediaQueryHandler
    : AizenQueryHandler<GetAdminVesselMediaQuery, GetVesselMediaResponse>
{
    private readonly IVesselRemoteCall _vessel;

    public GetAdminVesselMediaQueryHandler(
        IVesselRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<GetVesselMediaResponse?> Handle(
        GetAdminVesselMediaQuery request, CancellationToken cancellationToken)
    {

        var result = await _vessel.GetVesselMedia(
            request.VesselId,
            request.PageIndex,
            request.PageSize);

        return result.Body;
    }
}
