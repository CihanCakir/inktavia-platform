using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Query;

[DocumentationInfo("GetAdminVesselById query handler", "Returns full vessel detail by ID from the Vessel module.")]
public sealed class GetVesselByIdBffQueryHandler : AizenQueryHandler<GetVesselByIdBffQuery, GetVesselDetailResponse>
{
    private readonly IVesselRemoteCall _vessel;
    public GetVesselByIdBffQueryHandler(
        IVesselRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<GetVesselDetailResponse?> Handle(GetVesselByIdBffQuery request, CancellationToken ct)
    {

        var r = await _vessel.GetVesselById(request.VesselId);
        return r.Body;
    }
}
