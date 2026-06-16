using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Media;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel media query handler", "Fetches vessel media list from the Vessel module.")]
public sealed class GetAdminVesselMediaQueryHandler
    : AizenQueryHandler<GetAdminVesselMediaQuery, GetVesselMediaResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminVesselMediaQueryHandler(
        IVesselAdminBffRemoteCall vessel,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _vessel = vessel;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<GetVesselMediaResponse?> Handle(
        GetAdminVesselMediaQuery request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _vessel.GetVesselMedia(
            request.VesselId, authHeader, request.UserToken,
            request.PageIndex, request.PageSize);

        return result.Body;
    }
}
