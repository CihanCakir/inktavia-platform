using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

[DocumentationInfo("Restore vessel command handler", "Restores an archived vessel via the Vessel module.")]
public sealed class RestoreVesselCommandHandler
    : AizenCommandHandler<RestoreVesselCommand, RestoreVesselResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public RestoreVesselCommandHandler(IVesselAdminBffRemoteCall vessel,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _vessel = vessel;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<RestoreVesselResponse?> Handle(
        RestoreVesselCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _vessel.RestoreVessel(request.VesselId, authHeader, request.UserToken);
        return result.Body;
    }
}
