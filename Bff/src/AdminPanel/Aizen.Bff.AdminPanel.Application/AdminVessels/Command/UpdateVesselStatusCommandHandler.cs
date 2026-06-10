using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

[DocumentationInfo("Update vessel status command handler", "Changes the operational status of a vessel via the Vessel module.")]
public sealed class UpdateVesselStatusCommandHandler
    : AizenCommandHandler<UpdateVesselStatusCommand, UpdateVesselStatusResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public UpdateVesselStatusCommandHandler(IVesselAdminBffRemoteCall vessel,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _vessel = vessel;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<UpdateVesselStatusResponse?> Handle(
        UpdateVesselStatusCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _vessel.UpdateVesselStatus(request.VesselId, request.Payload, authHeader, request.UserToken);
        return result.Body;
    }
}
