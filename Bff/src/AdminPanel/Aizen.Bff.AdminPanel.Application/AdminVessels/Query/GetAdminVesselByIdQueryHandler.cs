using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("GetAdminVesselById query handler", "Returns full vessel detail by ID from the Vessel module.")]
public sealed class GetAdminVesselByIdQueryHandler : AizenQueryHandler<GetAdminVesselByIdQuery, GetVesselDetailResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    public GetAdminVesselByIdQueryHandler(
        IVesselAdminBffRemoteCall vessel,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _vessel = vessel;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<GetVesselDetailResponse?> Handle(GetAdminVesselByIdQuery request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var r = await _vessel.GetVesselById(request.VesselId, authHeader, request.UserToken);
        return r.Body;
    }
}
